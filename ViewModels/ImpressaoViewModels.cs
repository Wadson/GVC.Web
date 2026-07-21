using GVC.Web.DTOs;
using GVC.Web.Models;

namespace GVC.Web.ViewModels;

public sealed class EmpresaImpressaoViewModel
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? LogoDataUri { get; set; }
}

public sealed class ReciboPagamentoViewModel
{
    public EmpresaImpressaoViewModel Empresa { get; set; } = new();
    public int PagamentoId { get; set; }
    public int VendaId { get; set; }
    public int NumeroParcela { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string? ClienteDocumento { get; set; }
    public decimal ValorPago { get; set; }
    public decimal Juros { get; set; }
    public decimal Multa { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public DateTime DataPagamento { get; set; }
    public string? Observacao { get; set; }
}

public sealed class ComprovanteVendaViewModel
{
    public EmpresaImpressaoViewModel Empresa { get; set; } = new();
    public int VendaId { get; set; }
    public DateTime DataVenda { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string? ClienteDocumento { get; set; }
    public string? VendedorNome { get; set; }
    public StatusVenda Status { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal TotalDesconto { get; set; }
    public decimal TotalLiquido { get; set; }
    public string? Observacoes { get; set; }
    public List<ComprovanteVendaItemViewModel> Itens { get; set; } = [];
    public List<ComprovanteVendaParcelaViewModel> Parcelas { get; set; } = [];
}

public sealed record ComprovanteVendaItemViewModel(
    string Codigo, string Produto, int Quantidade, decimal PrecoUnitario,
    decimal Desconto, decimal Subtotal);

public sealed record ComprovanteVendaParcelaViewModel(
    int Numero, DateTime Vencimento, decimal Valor, StatusParcela Status);

public sealed class FechamentoCaixaViewModel
{
    public EmpresaImpressaoViewModel Empresa { get; set; } = new();
    public int CaixaId { get; set; }
    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }
    public string UsuarioAbertura { get; set; } = string.Empty;
    public string? UsuarioFechamento { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SaldoInicial { get; set; }
    public decimal SaldoFinalSistema { get; set; }
    public decimal SaldoFinalInformado { get; set; }
    public decimal Diferenca { get; set; }
    public List<FechamentoCaixaFormaViewModel> TotaisPorForma { get; set; } = [];
    public List<FechamentoCaixaMovimentoViewModel> SangriasSuprimentos { get; set; } = [];
}

public sealed record FechamentoCaixaFormaViewModel(string FormaPagamento, decimal Entradas, decimal Saidas);
public sealed record FechamentoCaixaMovimentoViewModel(DateTime DataHora, string Tipo, string Historico, decimal Valor, string FormaPagamento);

public sealed class RelatorioEstoqueViewModel
{
    public EmpresaImpressaoViewModel Empresa { get; set; } = new();
    public DateTime EmitidoEm { get; set; } = DateTime.Now;
    public List<RelatorioEstoqueItemViewModel> Itens { get; set; } = [];
    public decimal TotalCusto => Itens.Sum(x => x.ValorCusto);
    public decimal TotalVenda => Itens.Sum(x => x.ValorVenda);
}

public sealed class RelatorioEstoqueItemViewModel
{
    public string Categoria { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Produto { get; set; } = string.Empty;
    public int Estoque { get; set; }
    public decimal PrecoCusto { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal ValorCusto => Estoque * PrecoCusto;
    public decimal ValorVenda => Estoque * PrecoVenda;
}

public sealed class RelatorioInadimplenciaViewModel
{
    public EmpresaImpressaoViewModel Empresa { get; set; } = new();
    public int? ClienteId { get; set; }
    public DateTime? VencimentoInicial { get; set; }
    public DateTime? VencimentoFinal { get; set; }
    public string? Status { get; set; }
    public List<EntradaEstoqueOptionDTO> Clientes { get; set; } = [];
    public List<RelatorioInadimplenciaClienteViewModel> ClientesAgrupados { get; set; } = [];
    public decimal TotalVencido => ClientesAgrupados.Sum(x => x.TotalVencido);
    public decimal TotalAVencer => ClientesAgrupados.Sum(x => x.TotalAVencer);
    public decimal SaldoDevedor => ClientesAgrupados.Sum(x => x.SaldoDevedor);
}

public sealed class RelatorioInadimplenciaClienteViewModel
{
    public string Cliente { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public decimal TotalVencido { get; set; }
    public decimal TotalAVencer { get; set; }
    public decimal SaldoDevedor { get; set; }
    public List<RelatorioInadimplenciaParcelaViewModel> Parcelas { get; set; } = [];
}

public sealed record RelatorioInadimplenciaParcelaViewModel(
    int VendaId, int Numero, DateTime Vencimento, decimal Valor,
    decimal Recebido, decimal Saldo, string Status);

public sealed class ComprovanteComissaoViewModel
{
    public EmpresaImpressaoViewModel Empresa { get; set; } = new();
    public int PagamentoComissaoId { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public DateTime DataInicial { get; set; }
    public DateTime DataFinal { get; set; }
    public decimal TotalVendas { get; set; }
    public decimal PercentualComissao { get; set; }
    public decimal ValorPago { get; set; }
    public DateTime DataPagamento { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Observacoes { get; set; }
}

public sealed class CentralRelatoriosViewModel
{
    public List<DocumentoRecenteViewModel> Vendas { get; set; } = [];
    public List<DocumentoRecenteViewModel> Recibos { get; set; } = [];
    public List<DocumentoRecenteViewModel> Caixas { get; set; } = [];
    public List<DocumentoRecenteViewModel> Comissoes { get; set; } = [];
}

public sealed record DocumentoRecenteViewModel(int Id, string Descricao, DateTime Data, decimal Valor);
