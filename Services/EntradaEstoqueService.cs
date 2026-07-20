using System.Data;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using GVC.Web.Data;
using GVC.Web.DTOs;
using GVC.Web.Extensions;
using GVC.Web.Models;
using GVC.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Services;

public sealed class EntradaEstoqueService(ErpDbContext db) : IEntradaEstoqueService
{
    private const long MaxXmlSize = 10 * 1024 * 1024;

    public async Task<EntradaEstoqueViewModel> ProcessarXmlAsync(
        int empresaId,
        Stream xmlStream,
        CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await xmlStream.CopyToAsync(memory, cancellationToken);
        if (memory.Length == 0 || memory.Length > MaxXmlSize)
        {
            throw new InvalidOperationException("O XML deve possuir conteúdo e ter no máximo 10 MB.");
        }

        memory.Position = 0;
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxXmlSize
        };

        XDocument xml;
        using (XmlReader reader = XmlReader.Create(memory, settings))
        {
            xml = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }

        XElement? infNfe = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe");
        XElement? emit = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "emit");
        XElement? ide = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "ide");
        if (infNfe is null || emit is null || ide is null)
        {
            throw new InvalidOperationException("O arquivo não possui a estrutura esperada de uma NF-e.");
        }

        string cnpj = Valor(emit, "CNPJ").OnlyDigits();
        var fornecedores = await db.Fornecedores
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .Select(x => new { x.FornecedorId, x.Nome, x.Cnpj })
            .ToListAsync(cancellationToken);
        var fornecedor = fornecedores.FirstOrDefault(x => x.Cnpj.OnlyDigits() == cnpj);

        var detalhes = xml.Descendants().Where(x => x.Name.LocalName == "det").ToList();
        if (detalhes.Count == 0)
        {
            throw new InvalidOperationException("A NF-e não contém itens.");
        }

        var codigos = detalhes
            .Select(x => Valor(Filho(x, "prod"), "cProd"))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var mapeamentos = fornecedor is null
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : await db.ProdutosFornecedoresMapeamentos
                .AsNoTracking()
                .Where(x => x.EmpresaId == empresaId &&
                            x.FornecedorId == fornecedor.FornecedorId &&
                            codigos.Contains(x.CodigoNoFornecedor))
                .ToDictionaryAsync(x => x.CodigoNoFornecedor, x => x.ProdutoId, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var itens = detalhes.Select(detalhe =>
        {
            XElement prod = Filho(detalhe, "prod")
                ?? throw new InvalidOperationException("Existe um item sem o grupo <prod>.");
            decimal quantidade = Decimal(prod, "qCom");
            if (quantidade <= 0 || quantidade != decimal.Truncate(quantidade))
            {
                throw new InvalidOperationException("A estrutura atual do estoque aceita somente quantidades inteiras.");
            }

            string codigo = Valor(prod, "cProd");
            decimal precoCompra = Decimal(prod, "vUnCom");
            mapeamentos.TryGetValue(codigo, out int produtoId);

            return new EntradaEstoqueItemDTO
            {
                ProdutoId = produtoId,
                CodigoFornecedor = codigo,
                DescricaoFornecedor = Valor(prod, "xProd"),
                Quantidade = decimal.ToInt32(quantidade),
                PrecoUnitarioCompra = precoCompra,
                PrecoCustoUnitario = precoCompra,
                ValorTotalItem = Decimal(prod, "vProd"),
                Cfop = Valor(prod, "CFOP"),
                Ncm = Valor(prod, "NCM")
            };
        }).ToList();

        XElement? total = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "ICMSTot");
        DateTime emissao = Data(Valor(ide, "dhEmi")) ?? Data(Valor(ide, "dEmi")) ?? DateTime.Today;
        decimal valorNota = total is null ? itens.Sum(x => x.ValorTotalItem) : Decimal(total, "vNF");

        var parcelas = xml.Descendants()
            .Where(x => x.Name.LocalName == "dup")
            .Select(x => new ParcelaEntradaDTO
            {
                DataVencimento = Data(Valor(x, "dVenc")) ?? emissao.Date,
                Valor = Decimal(x, "vDup")
            })
            .Where(x => x.Valor > 0)
            .ToList();

        if (parcelas.Count == 0 && valorNota > 0)
        {
            parcelas.Add(new ParcelaEntradaDTO
            {
                DataVencimento = emissao.Date.AddDays(30),
                Valor = valorNota
            });
        }

        string chave = infNfe.Attribute("Id")?.Value ?? string.Empty;
        if (chave.StartsWith("NFe", StringComparison.OrdinalIgnoreCase))
        {
            chave = chave[3..];
        }

        return new EntradaEstoqueViewModel
        {
            OrigemXml = true,
            FornecedorId = fornecedor?.FornecedorId ?? 0,
            FornecedorXml = $"{Valor(emit, "xNome")} — CNPJ {cnpj}",
            NumeroDocumento = Valor(ide, "nNF"),
            Serie = Valor(ide, "serie"),
            ChaveAcesso = chave,
            DataEmissao = emissao.Date,
            ValorTotalProdutos = total is null ? itens.Sum(x => x.ValorTotalItem) : Decimal(total, "vProd"),
            ValorFrete = total is null ? 0 : Decimal(total, "vFrete"),
            ValorDesconto = total is null ? 0 : Decimal(total, "vDesc"),
            ValorTotalNota = valorNota,
            XmlConteudo = xml.ToString(SaveOptions.DisableFormatting),
            Itens = itens,
            Parcelas = parcelas
        };
    }

    public async Task<int> SalvarAsync(
        int empresaId,
        int usuarioId,
        string usuarioNome,
        EntradaEstoqueViewModel input,
        CancellationToken cancellationToken)
    {
        ValidarInput(input);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var fornecedor = await db.Fornecedores
            .SingleOrDefaultAsync(x => x.FornecedorId == input.FornecedorId && x.EmpresaId == empresaId, cancellationToken)
            ?? throw new InvalidOperationException("Fornecedor inválido para a empresa ativa.");

        if (!await db.PlanosContas.AnyAsync(x => x.PlanoContasId == input.PlanoContasId && x.EmpresaId == empresaId, cancellationToken))
        {
            throw new InvalidOperationException("Plano de contas inválido para a empresa ativa.");
        }

        Pedido? pedidoOrigem = null;
        if (input.PedidoId.HasValue)
        {
            pedidoOrigem = await db.Pedidos.SingleOrDefaultAsync(
                x => x.PedidoId == input.PedidoId.Value &&
                     x.EmpresaId == empresaId &&
                     x.FornecedorId == fornecedor.FornecedorId &&
                     (x.Status == "Pendente" || x.Status == "Aguardando Entrega"),
                cancellationToken);

            if (pedidoOrigem is null)
            {
                throw new InvalidOperationException(
                    "Pedido de origem inválido, já concluído ou pertencente a outro fornecedor/empresa.");
            }
        }

        if (await db.DocumentosEntrada.AnyAsync(x => x.EmpresaId == empresaId &&
                                                       x.FornecedorId == fornecedor.FornecedorId &&
                                                       x.NumeroDocumento == input.NumeroDocumento &&
                                                       x.Serie == input.Serie,
                cancellationToken))
        {
            throw new InvalidOperationException("Este documento de entrada já foi lançado para o fornecedor.");
        }

        int[] produtosIds = input.Itens.Select(x => x.ProdutoId).Distinct().ToArray();
        var produtos = await db.Produtos
            .Where(x => x.EmpresaId == empresaId && produtosIds.Contains(x.ProdutoId))
            .ToDictionaryAsync(x => x.ProdutoId, cancellationToken);
        if (produtos.Count != produtosIds.Length)
        {
            throw new InvalidOperationException("Um ou mais produtos não pertencem à empresa ativa.");
        }

        decimal totalProdutos = input.Itens.Sum(x => x.Quantidade * x.PrecoUnitarioCompra);
        decimal totalCalculado = totalProdutos + input.ValorFrete - input.ValorDesconto;
        decimal totalNota = input.ValorTotalNota > 0 ? input.ValorTotalNota : totalCalculado;
        decimal totalParcelas = input.Parcelas.Sum(x => x.Valor);
        if (Math.Abs(totalParcelas - totalNota) > 0.01m)
        {
            throw new InvalidOperationException("A soma das parcelas deve ser igual ao valor total da nota.");
        }

        var documento = new DocumentoEntrada
        {
            EmpresaId = empresaId,
            FornecedorId = fornecedor.FornecedorId,
            PedidoId = input.PedidoId,
            TipoEntrada = input.OrigemXml ? "XML" : "MANUAL",
            NumeroDocumento = input.NumeroDocumento.Trim(),
            Serie = input.Serie?.Trim(),
            ChaveAcesso = string.IsNullOrWhiteSpace(input.ChaveAcesso) ? null : input.ChaveAcesso.Trim(),
            DataEmissao = input.DataEmissao.Date,
            DataEntrada = DateTime.Now,
            ValorTotalProdutos = totalProdutos,
            ValorFrete = input.ValorFrete,
            ValorDesconto = input.ValorDesconto,
            ValorTotalNota = totalNota,
            XmlConteudo = input.OrigemXml ? input.XmlConteudo : null,
            Observacao = input.Observacao,
            UsuarioId = usuarioId
        };

        db.DocumentosEntrada.Add(documento);
        await db.SaveChangesAsync(cancellationToken);

        var agora = DateTime.Now;
        foreach (EntradaEstoqueItemDTO item in input.Itens)
        {
            Produto produto = produtos[item.ProdutoId];
            int estoqueAnterior = produto.Estoque;
            produto.Estoque += item.Quantidade;
            produto.PrecoCompra = item.PrecoUnitarioCompra;
            produto.PrecoCusto = item.PrecoCustoUnitario;
            produto.DataDeEntrada = agora;
            produto.FornecedorId = fornecedor.FornecedorId;

            db.DocumentosEntradaItens.Add(new DocumentoEntradaItem
            {
                DocumentoEntradaId = documento.DocumentoEntradaId,
                ProdutoId = produto.ProdutoId,
                Quantidade = item.Quantidade,
                PrecoUnitarioCompra = item.PrecoUnitarioCompra,
                PrecoCustoUnitario = item.PrecoCustoUnitario,
                ValorTotalItem = item.Quantidade * item.PrecoUnitarioCompra,
                Cfop = item.Cfop,
                Ncm = item.Ncm
            });

            db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                ProdutoId = produto.ProdutoId,
                TipoMovimentacao = "ENTRADA",
                Quantidade = item.Quantidade,
                EstoqueAnterior = estoqueAnterior,
                EstoqueAtual = produto.Estoque,
                Origem = "DOC_ENTRADA",
                Documento = documento.NumeroDocumento,
                DataMovimentacao = agora,
                Usuario = usuarioNome,
                EmpresaId = empresaId,
                PrecoCompra = item.PrecoUnitarioCompra,
                PrecoCustoEntrada = item.PrecoCustoUnitario,
                FornecedorId = fornecedor.FornecedorId
            });
        }

        if (input.OrigemXml)
        {
            await AtualizarMapeamentosAsync(empresaId, fornecedor.FornecedorId, input.Itens, cancellationToken);
        }

        for (int indice = 0; indice < input.Parcelas.Count; indice++)
        {
            ParcelaEntradaDTO parcela = input.Parcelas[indice];
            db.ContasAPagar.Add(new ContaAPagar
            {
                EmpresaId = empresaId,
                DocumentoEntradaId = documento.DocumentoEntradaId,
                PedidoId = input.PedidoId,
                FornecedorId = fornecedor.FornecedorId,
                PlanoContasId = input.PlanoContasId,
                Descricao = $"Entrada {documento.NumeroDocumento} - parcela {indice + 1}/{input.Parcelas.Count}",
                NumeroDocumento = documento.NumeroDocumento,
                DataEmissao = documento.DataEmissao,
                DataVencimento = parcela.DataVencimento.Date,
                Valor = parcela.Valor,
                Status = "Pendente"
            });
        }

        if (pedidoOrigem is not null)
        {
            pedidoOrigem.Status = "Concluído";
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return documento.DocumentoEntradaId;
    }

    private async Task AtualizarMapeamentosAsync(
        int empresaId,
        int fornecedorId,
        IEnumerable<EntradaEstoqueItemDTO> itens,
        CancellationToken cancellationToken)
    {
        string[] codigos = itens
            .Where(x => !string.IsNullOrWhiteSpace(x.CodigoFornecedor))
            .Select(x => x.CodigoFornecedor!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var existentes = await db.ProdutosFornecedoresMapeamentos
            .Where(x => x.EmpresaId == empresaId &&
                        x.FornecedorId == fornecedorId &&
                        codigos.Contains(x.CodigoNoFornecedor))
            .ToDictionaryAsync(x => x.CodigoNoFornecedor, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (EntradaEstoqueItemDTO item in itens.Where(x => !string.IsNullOrWhiteSpace(x.CodigoFornecedor)))
        {
            string codigo = item.CodigoFornecedor!.Trim();
            if (existentes.TryGetValue(codigo, out ProdutoFornecedorMapeamento? existente))
            {
                existente.ProdutoId = item.ProdutoId;
            }
            else
            {
                var novo = new ProdutoFornecedorMapeamento
                {
                    EmpresaId = empresaId,
                    FornecedorId = fornecedorId,
                    CodigoNoFornecedor = codigo,
                    ProdutoId = item.ProdutoId
                };
                db.ProdutosFornecedoresMapeamentos.Add(novo);
                existentes[codigo] = novo;
            }
        }
    }

    private static void ValidarInput(EntradaEstoqueViewModel input)
    {
        if (string.IsNullOrWhiteSpace(input.NumeroDocumento))
            throw new InvalidOperationException("Informe o número do documento.");
        if (input.DataEmissao == default)
            throw new InvalidOperationException("Informe a data de emissão.");
        if (input.Itens.Count == 0 || input.Itens.Any(x => x.ProdutoId <= 0 || x.Quantidade <= 0 || x.PrecoUnitarioCompra <= 0 || x.PrecoCustoUnitario <= 0))
            throw new InvalidOperationException("Revise os produtos, quantidades e preços da entrada.");
        if (input.Parcelas.Count == 0 || input.Parcelas.Any(x => x.Valor <= 0 || x.DataVencimento == default))
            throw new InvalidOperationException("Gere ao menos uma parcela válida.");
        if (input.ValorFrete < 0 || input.ValorDesconto < 0)
            throw new InvalidOperationException("Frete e desconto não podem ser negativos.");
    }

    private static XElement? Filho(XElement? elemento, string nome) =>
        elemento?.Elements().FirstOrDefault(x => x.Name.LocalName == nome);

    private static string Valor(XElement? elemento, string nome) => Filho(elemento, nome)?.Value.Trim() ?? string.Empty;

    private static decimal Decimal(XElement elemento, string nome) =>
        decimal.TryParse(Valor(elemento, nome), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor) ? valor : 0;

    private static DateTime? Data(string valor) =>
        DateTimeOffset.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dataHora)
            ? dataHora.DateTime
            : DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data)
                ? data
                : null;
}
