using System.Data;
using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Services;

public sealed class VendaService(ErpDbContext db) : IVendaService
{
    public async Task<long> FinalizarAsync(int empresaId, int usuarioId, FinalizarVendaInput input, CancellationToken ct)
    {
        if (input.ClienteId is null)
            throw new InvalidOperationException("Informe o cliente.");

        if (input.Itens is null || input.Itens.Count == 0)
            throw new InvalidOperationException("Adicione ao menos um item.");

        if (input.Parcelas is null || input.Parcelas.Count == 0)
            throw new InvalidOperationException("Gere ao menos uma parcela.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        if (!await db.Clientes.AnyAsync(x => x.ClienteId == input.ClienteId && x.EmpresaId == empresaId && x.Status == 1, ct))
            throw new InvalidOperationException("Cliente inválido.");

        if (input.VendedorId.HasValue && !await db.Vendedores.AnyAsync(x => x.VendedorId == input.VendedorId && x.EmpresaId == empresaId && x.Status == 1, ct))
            throw new InvalidOperationException("Vendedor inválido.");

        if (!await db.FormasPagamento.AnyAsync(x => x.FormaPgtoId == input.FormaPagamentoId && x.Ativo, ct))
            throw new InvalidOperationException("Forma de pagamento inválida.");

        var caixa = await db.Caixas.SingleOrDefaultAsync(x => x.EmpresaId == empresaId && x.Status == "Aberto", ct)
            ?? throw new InvalidOperationException("Abra o caixa antes de concluir a venda.");

        var ids = input.Itens.Select(x => x.ProdutoId).Distinct().ToArray();

        var produtos = await db.Produtos.Where(x => x.EmpresaId == empresaId && (x.Status == "Ativo" || x.Status == "Disponível") && ids.Contains(x.ProdutoId)).ToDictionaryAsync(x => x.ProdutoId, ct);

        if (produtos.Count != ids.Length)
            throw new InvalidOperationException("Um ou mais produtos são inválidos.");

        var venda = new Venda { EmpresaId = empresaId, ClienteId = input.ClienteId.Value, VendedorId = input.VendedorId, FormaPgtoId = input.FormaPagamentoId, DataVenda = DateTime.Now };

        var movimentos = new List<MovimentacaoEstoque>();

        foreach (var item in input.Itens)
        {
            if (item.Quantidade <= 0 || item.Quantidade != decimal.Truncate(item.Quantidade))
                throw new InvalidOperationException("A quantidade deve ser um número inteiro positivo.");

            var quantidade = (int)item.Quantidade;

            var produto = produtos[item.ProdutoId];

            if (produto.Estoque < quantidade)
                throw new InvalidOperationException($"Estoque insuficiente para {produto.NomeProduto}.");

            var bruto = produto.PrecoDeVenda * quantidade;

            var desconto = Math.Clamp(item.Desconto, 0, bruto);

            var estoqueAnterior = produto.Estoque;

            produto.Estoque -= quantidade;

            venda.Itens.Add(new ItemVenda { ProdutoId = produto.ProdutoId, EmpresaId = empresaId, Quantidade = quantidade, PrecoUnitario = produto.PrecoDeVenda, DescontoItem = desconto });

            movimentos.Add(new MovimentacaoEstoque { ProdutoId = produto.ProdutoId, EmpresaId = empresaId, TipoMovimentacao = "SAIDA", Quantidade = quantidade, EstoqueAnterior = estoqueAnterior, EstoqueAtual = produto.Estoque, Origem = "Venda", Usuario = usuarioId.ToString(), DataMovimentacao = DateTime.Now });
        }

        venda.TotalBruto = venda.Itens.Sum(x => x.PrecoUnitario * x.Quantidade);

        var descontoItens = venda.Itens.Sum(x => x.DescontoItem ?? 0);

        venda.TotalDesconto = Math.Clamp(input.Desconto + descontoItens, 0, venda.TotalBruto);

        venda.TotalLiquido = venda.TotalBruto - venda.TotalDesconto;

        var parcelas = input.Parcelas.OrderBy(x => x.Numero).ToArray();

        if (parcelas.Any(x => x.Numero <= 0 || x.DataVencimento.Date < DateTime.Today || x.Valor <= 0))
            throw new InvalidOperationException("Revise os números, valores e vencimentos das parcelas.");

        if (parcelas.Select(x => x.Numero).Distinct().Count() != parcelas.Length)
            throw new InvalidOperationException("Existem números de parcela repetidos.");

        if (Math.Abs(parcelas.Sum(x => x.Valor) - venda.TotalLiquido) > 0.01m)
            throw new InvalidOperationException("A soma das parcelas deve ser igual ao total da venda.");

        db.Vendas.Add(venda);

        await db.SaveChangesAsync(ct);

        foreach (var movimento in movimentos)
            movimento.Documento = venda.VendaId.ToString();

        db.MovimentacoesEstoque.AddRange(movimentos);

        foreach (var item in parcelas)
        {
            var parcela = new Parcela { VendaId = venda.VendaId, EmpresaId = empresaId, NumeroParcela = item.Numero, DataVencimento = item.DataVencimento.Date, ValorParcela = item.Valor, Status = input.RecebidoAgora ? "Pago" : "Pendente", ValorRecebido = input.RecebidoAgora ? item.Valor : null, DataPagamento = input.RecebidoAgora ? DateTime.Today : null };

            db.Parcelas.Add(parcela);

            await db.SaveChangesAsync(ct);

            if (input.RecebidoAgora)
                db.PagamentosParciais.Add(new PagamentoParcial { ParcelaId = parcela.ParcelaId, EmpresaId = empresaId, ValorPago = item.Valor, DataPagamento = DateTime.Today, FormaPgtoId = input.FormaPagamentoId, Observacao = $"Recebimento da venda #{venda.VendaId}" });
        }

        if (input.RecebidoAgora)
            db.CaixaMovimentos.Add(new CaixaMovimento { CaixaId = caixa.CaixaId, EmpresaId = empresaId, UsuarioId = usuarioId, FormaPgtoId = input.FormaPagamentoId, Tipo = "ENTRADA", Valor = venda.TotalLiquido, Historico = $"Venda #{venda.VendaId}", Origem = "Venda", ReferenciaId = venda.VendaId, DataHora = DateTime.Now });

        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return venda.VendaId;
    }
}