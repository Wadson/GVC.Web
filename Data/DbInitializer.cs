using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Data;

public static class DbInitializer
{
    private static readonly (string Codigo, string Descricao, string Tipo)[] PlanosPadrao =
    [
        ("1", "RECEITAS", "R"),
        ("1.01", "Venda de Mercadorias", "R"),
        ("1.02", "Outras Receitas", "R"),
        ("2", "DESPESAS E CUSTOS", "D"),
        ("2.01", "Compras de Mercadorias para Revenda", "D"),
        ("2.02", "Despesas com Pessoal e Pró-labore", "D"),
        ("2.03", "Aluguel, Água e Energia", "D"),
        ("2.04", "Impostos e Taxas", "D")
    ];

    public static async Task<int> SeedPlanoContasAsync(
        ErpDbContext db,
        int empresaId,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0 || await db.PlanosContas.AnyAsync(x => x.EmpresaId == empresaId, cancellationToken))
        {
            return 0;
        }

        var planos = PlanosPadrao.Select(x => new PlanoContas
        {
            EmpresaId = empresaId,
            CodigoClassificacao = x.Codigo,
            Descricao = x.Descricao,
            Tipo = x.Tipo
        });

        await db.PlanosContas.AddRangeAsync(planos, cancellationToken);
        return await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedPlanosContasAsync(
        ErpDbContext db,
        CancellationToken cancellationToken = default)
    {
        int[] empresasIds = await db.Empresas.AsNoTracking()
            .Select(x => x.EmpresaId)
            .ToArrayAsync(cancellationToken);

        foreach (int empresaId in empresasIds)
        {
            await SeedPlanoContasAsync(db, empresaId, cancellationToken);
        }
    }

    public static async Task<bool> ApplyDataVersionAsync(
        ErpDbContext db,
        string version,
        string description,
        Func<ErpDbContext, CancellationToken, Task> apply,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(apply);

        if (await db.DataVersions.AsNoTracking().AnyAsync(x => x.Version == version, cancellationToken))
            return false;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await apply(db, cancellationToken);
        db.DataVersions.Add(new DataVersion
        {
            Version = version.Trim(),
            Description = description?.Trim() ?? string.Empty,
            AppliedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
