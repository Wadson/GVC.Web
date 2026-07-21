using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Data;

public class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<Estado> Estados => Set<Estado>();

    public DbSet<Cidade> Cidades => Set<Cidade>();

    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<ConfiguracaoPix> ConfiguracoesPix => Set<ConfiguracaoPix>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<TokenRedefinicao> TokensRedefinicao => Set<TokenRedefinicao>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();

    public DbSet<Vendedor> Vendedores => Set<Vendedor>();

    public DbSet<Marca> Marcas => Set<Marca>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<ProdutoVariacao> ProdutosVariacoes => Set<ProdutoVariacao>();

    public DbSet<ProdutoVariacaoAtributo> ProdutosVariacoesAtributos => Set<ProdutoVariacaoAtributo>();

    public DbSet<Promocao> Promocoes => Set<Promocao>();

    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    public DbSet<FormaPagamento> FormasPagamento => Set<FormaPagamento>();

    public DbSet<Caixa> Caixas => Set<Caixa>();

    public DbSet<CaixaMovimento> CaixaMovimentos => Set<CaixaMovimento>();

    public DbSet<Venda> Vendas => Set<Venda>();

    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();

    public DbSet<FiscalDocumento> DocumentosFiscais => Set<FiscalDocumento>();

    public DbSet<Parcela> Parcelas => Set<Parcela>();

    public DbSet<PagamentoParcial> PagamentosParciais => Set<PagamentoParcial>();

    public DbSet<BoletoBancario> BoletosBancarios => Set<BoletoBancario>();

    public DbSet<PlanoContas> PlanosContas => Set<PlanoContas>();

    public DbSet<ContaAPagar> ContasAPagar => Set<ContaAPagar>();

    public DbSet<PagamentoComissaoVendedor> PagamentosComissao => Set<PagamentoComissaoVendedor>();

    public DbSet<PermissaoUsuario> PermissoesUsuario => Set<PermissaoUsuario>();

    public DbSet<DocumentoEntrada> DocumentosEntrada => Set<DocumentoEntrada>();

    public DbSet<DocumentoEntradaItem> DocumentosEntradaItens => Set<DocumentoEntradaItem>();

    public DbSet<ProdutoFornecedorMapeamento> ProdutosFornecedoresMapeamentos => Set<ProdutoFornecedorMapeamento>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Cidade>().HasOne(x => x.Estado).WithMany(x => x.Cidades).HasForeignKey(x => x.EstadoId).OnDelete(DeleteBehavior.Restrict);

        Rel<Empresa, Cidade>(b, x => x.CidadeId);

        Rel<ConfiguracaoPix, Empresa>(b, x => x.EmpresaId);

        Rel<Usuario, Empresa>(b, x => x.EmpresaId);

        Rel<TokenRedefinicao, Usuario>(b, x => x.UsuarioId);

        Rel<Cliente, Cidade>(b, x => x.CidadeId);

        Rel<Cliente, Empresa>(b, x => x.EmpresaId);

        Rel<Fornecedor, Cidade>(b, x => x.CidadeId);

        Rel<Fornecedor, Empresa>(b, x => x.EmpresaId);

        Rel<Vendedor, Cidade>(b, x => x.CidadeId);

        Rel<Vendedor, Empresa>(b, x => x.EmpresaId);

        Rel<Marca, Empresa>(b, x => x.EmpresaId);

        Rel<Categoria, Empresa>(b, x => x.EmpresaId);

        Rel<Produto, Fornecedor>(b, x => x.FornecedorId);

        Rel<Produto, Marca>(b, x => x.MarcaId);

        Rel<Produto, Categoria>(b, x => x.CategoriaId);

        Rel<Produto, Empresa>(b, x => x.EmpresaId);

        b.Entity<Produto>()
            .HasMany(x => x.Variacoes)
            .WithOne(x => x.Produto)
            .HasForeignKey(x => x.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProdutoVariacao>()
            .HasMany(x => x.Atributos)
            .WithOne(x => x.Variacao)
            .HasForeignKey(x => x.VariacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProdutoVariacao>().HasIndex(x => x.ProdutoId);
        b.Entity<ProdutoVariacao>().HasIndex(x => x.GtinEan);

        Rel<Promocao, Produto>(b, x => x.ProdutoId);

        Rel<MovimentacaoEstoque, Produto>(b, x => x.ProdutoId);

        b.Entity<MovimentacaoEstoque>()
            .HasOne(x => x.Variacao)
            .WithMany()
            .HasForeignKey(x => x.VariacaoID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        Rel<MovimentacaoEstoque, Fornecedor>(b, x => x.FornecedorId);

        Rel<MovimentacaoEstoque, Empresa>(b, x => x.EmpresaId);

        Rel<Pedido, Fornecedor>(b, x => x.FornecedorId);

        Rel<Pedido, Empresa>(b, x => x.EmpresaId);

        b.Entity<ItemPedido>()
            .HasOne(x => x.Pedido)
            .WithMany(x => x.Itens)
            .HasForeignKey(x => x.PedidoId)
            .OnDelete(DeleteBehavior.Restrict);

        Rel<ItemPedido, Produto>(b, x => x.ProdutoId);

        b.Entity<Caixa>().HasOne(x => x.UsuarioAbertura).WithMany().HasForeignKey(x => x.UsuarioAberturaId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Caixa>().HasOne(x => x.UsuarioFechamento).WithMany().HasForeignKey(x => x.UsuarioFechamentoId).OnDelete(DeleteBehavior.Restrict);

        Rel<Caixa, Empresa>(b, x => x.EmpresaId);

        Rel<CaixaMovimento, Caixa>(b, x => x.CaixaId);

        Rel<CaixaMovimento, FormaPagamento>(b, x => x.FormaPgtoId);

        Rel<CaixaMovimento, Usuario>(b, x => x.UsuarioId);

        Rel<CaixaMovimento, Empresa>(b, x => x.EmpresaId);

        Rel<Venda, Cliente>(b, x => x.ClienteId);

        Rel<Venda, FormaPagamento>(b, x => x.FormaPgtoId);

        Rel<Venda, Vendedor>(b, x => x.VendedorId);

        Rel<Venda, Empresa>(b, x => x.EmpresaId);

        b.Entity<ItemVenda>()
            .HasOne(x => x.Venda)
            .WithMany(x => x.Itens)
            .HasForeignKey(x => x.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        Rel<ItemVenda, Produto>(b, x => x.ProdutoId);

        b.Entity<ItemVenda>()
            .HasOne(x => x.Variacao)
            .WithMany()
            .HasForeignKey(x => x.VariacaoID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        Rel<ItemVenda, Empresa>(b, x => x.EmpresaId);

        Rel<FiscalDocumento, Venda>(b, x => x.VendaId);

        Rel<FiscalDocumento, Empresa>(b, x => x.EmpresaId);

        Rel<Parcela, Venda>(b, x => x.VendaId);

        Rel<Parcela, Empresa>(b, x => x.EmpresaId);

        Rel<PagamentoParcial, Parcela>(b, x => x.ParcelaId);

        Rel<PagamentoParcial, FormaPagamento>(b, x => x.FormaPgtoId);

        Rel<PagamentoParcial, Empresa>(b, x => x.EmpresaId);

        Rel<BoletoBancario, Parcela>(b, x => x.ParcelaId);

        Rel<PlanoContas, Empresa>(b, x => x.EmpresaId);

        Rel<ContaAPagar, Empresa>(b, x => x.EmpresaId);

        Rel<ContaAPagar, Fornecedor>(b, x => x.FornecedorId);

        Rel<ContaAPagar, Pedido>(b, x => x.PedidoId);

        Rel<ContaAPagar, PlanoContas>(b, x => x.PlanoContasId);

        Rel<ContaAPagar, FormaPagamento>(b, x => x.FormaPgtoId);

        Rel<PagamentoComissaoVendedor, Vendedor>(b, x => x.VendedorId);

        Rel<PagamentoComissaoVendedor, Empresa>(b, x => x.EmpresaId);

        Rel<PagamentoComissaoVendedor, Usuario>(b, x => x.UsuarioId);

        Rel<PermissaoUsuario, Usuario>(b, x => x.UsuarioId);

        Rel<PermissaoUsuario, Empresa>(b, x => x.EmpresaId);

        Rel<DocumentoEntrada, Empresa>(b, x => x.EmpresaId);

        Rel<DocumentoEntrada, Fornecedor>(b, x => x.FornecedorId);

        b.Entity<DocumentoEntradaItem>()
            .HasOne(x => x.DocumentoEntrada)
            .WithMany(x => x.Itens)
            .HasForeignKey(x => x.DocumentoEntradaId)
            .OnDelete(DeleteBehavior.Restrict);

        Rel<DocumentoEntradaItem, Produto>(b, x => x.ProdutoId);

        b.Entity<DocumentoEntradaItem>()
            .HasOne(x => x.Variacao)
            .WithMany()
            .HasForeignKey(x => x.VariacaoID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        Rel<ProdutoFornecedorMapeamento, Empresa>(b, x => x.EmpresaId);

        Rel<ProdutoFornecedorMapeamento, Fornecedor>(b, x => x.FornecedorId);

        Rel<ProdutoFornecedorMapeamento, Produto>(b, x => x.ProdutoId);

        b.Entity<ContaAPagar>()
            .HasOne(x => x.DocumentoEntrada)
            .WithMany()
            .HasForeignKey(x => x.DocumentoEntradaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Usuario>().HasIndex(x => new { x.EmpresaId, x.NomeUsuario }).IsUnique();

        b.Entity<Produto>().HasIndex(x => new { x.EmpresaId, x.GtinEan });

        b.Entity<PermissaoUsuario>()
            .HasIndex(x => new { x.UsuarioId, x.EmpresaId, x.Modulo })
            .IsUnique();

        b.Entity<ProdutoFornecedorMapeamento>()
            .HasIndex(x => new { x.EmpresaId, x.FornecedorId, x.CodigoNoFornecedor })
            .IsUnique();

        b.Entity<PlanoContas>()
            .HasIndex(x => new { x.EmpresaId, x.CodigoClassificacao })
            .IsUnique();
    }

    private static void Rel<TDependent, TPrincipal>(ModelBuilder b, System.Linq.Expressions.Expression<Func<TDependent, object?>> fk, DeleteBehavior delete = DeleteBehavior.Restrict) where TDependent : class where TPrincipal : class
    {
        var navigation = typeof(TDependent).GetProperties().SingleOrDefault(x => x.PropertyType == typeof(TPrincipal))?.Name;

        var body = fk.Body is System.Linq.Expressions.UnaryExpression unary ? unary.Operand : fk.Body;

        var foreignKey = ((System.Linq.Expressions.MemberExpression)body).Member.Name;

        b.Entity<TDependent>().HasOne(typeof(TPrincipal), navigation).WithMany().HasForeignKey(foreignKey).OnDelete(delete);
    }
}
