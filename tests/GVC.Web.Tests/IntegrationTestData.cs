using GVC.Web.Data;
using GVC.Web.Models;

namespace GVC.Web.Tests;

internal static class IntegrationTestData
{
    public static async Task<(int ClienteId, int ProdutoId, int VariacaoGId, int VariacaoPId, int CaixaId)>
        SeedProdutoComVariacoesAsync(ErpDbContext db, bool abrirCaixa = true)
    {
        var cliente = new Cliente
        {
            EmpresaId = 1, Nome = "Cliente teste", TipoCliente = "PF", Cpf = "12345678901",
            Status = 1, DataCriacao = DateTime.Now
        };
        var produto = new Produto
        {
            EmpresaId = 1, NomeProduto = "Camiseta", Referencia = "CAM",
            TemVariacao = true, Estoque = 0, PrecoCusto = 20m, PrecoDeVenda = 50m,
            Status = "Ativo", Unidade = "UN", DataDeEntrada = DateTime.Now,
            Variacoes =
            [
                new ProdutoVariacao
                {
                    Sku = "G-AZUL", Estoque = 10, Status = "Ativo", DataCriacao = DateTime.Now,
                    Atributos = [new ProdutoVariacaoAtributo { NomeAtributo = "Tamanho", ValorAtributo = "G" }]
                },
                new ProdutoVariacao
                {
                    Sku = "P-AZUL", Estoque = 5, Status = "Ativo", DataCriacao = DateTime.Now,
                    Atributos = [new ProdutoVariacaoAtributo { NomeAtributo = "Tamanho", ValorAtributo = "P" }]
                }
            ]
        };
        db.AddRange(cliente, produto);
        var caixa = new Caixa
        {
            EmpresaId = 1, UsuarioAberturaId = 1, DataCaixa = DateTime.Today,
            DataAbertura = DateTime.Now, Status = "Aberto", SaldoInicial = 0
        };
        if (abrirCaixa) db.Caixas.Add(caixa);
        await db.SaveChangesAsync();
        return (cliente.ClienteId, produto.ProdutoId,
            produto.Variacoes.Single(x => x.Sku == "G-AZUL").VariacaoId,
            produto.Variacoes.Single(x => x.Sku == "P-AZUL").VariacaoId,
            caixa.CaixaId);
    }

    public static PlanoContas NovoPlanoDespesa() => new()
    {
        EmpresaId = 1, CodigoClassificacao = "2.01", Descricao = "Compras", Tipo = "D"
    };

    public static Fornecedor NovoFornecedor() => new()
    {
        EmpresaId = 1, Nome = "Fornecedor teste", CidadeId = 1, DataCriacao = DateTime.Now
    };
}
