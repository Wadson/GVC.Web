using System.Data;
using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Controllers;

[Authorize]
[Route("ContasAPagar")]
public class ContasAPagarController(ErpDbContext db) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        [FromQuery] ContasAPagarFiltroViewModel filtro,
        CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        filtro.Busca = NormalizarOpcional(filtro.Busca);
        filtro.Status = NormalizarOpcional(filtro.Status);

        IQueryable<ContaAPagar> baseQuery = db.ContasAPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (filtro.DataInicial.HasValue)
            baseQuery = baseQuery.Where(x => x.DataVencimento >= filtro.DataInicial.Value.Date);

        if (filtro.DataFinal.HasValue)
            baseQuery = baseQuery.Where(x => x.DataVencimento < filtro.DataFinal.Value.Date.AddDays(1));

        if (filtro.PlanoContasId.HasValue)
            baseQuery = baseQuery.Where(x => x.PlanoContasId == filtro.PlanoContasId.Value);

        if (filtro.Busca is not null)
        {
            string busca = filtro.Busca;
            baseQuery = baseQuery.Where(x =>
                x.Descricao.Contains(busca) ||
                (x.NumeroDocumento != null && x.NumeroDocumento.Contains(busca)) ||
                (x.Fornecedor != null && x.Fornecedor.Nome.Contains(busca)));
        }

        DateTime hoje = DateTime.Today;
        string[] statusEmAberto = ["Pendente", "Vencido", "Pago Parcial", "ParcialmentePago"];

        filtro.TotalAPagar = await baseQuery
            .Where(x => statusEmAberto.Contains(x.Status))
            .SumAsync(x => (decimal?)(x.Valor - (x.ValorPago ?? 0)), cancellationToken) ?? 0;

        filtro.TotalPago = await baseQuery
            .Where(x => x.Status == "Pago" || x.Status == "Pago Parcial" || x.Status == "ParcialmentePago")
            .SumAsync(x => x.ValorPago, cancellationToken) ?? 0;

        filtro.TotalVencido = await baseQuery
            .Where(x => statusEmAberto.Contains(x.Status) && x.DataVencimento.Date < hoje)
            .SumAsync(x => (decimal?)(x.Valor - (x.ValorPago ?? 0)), cancellationToken) ?? 0;

        IQueryable<ContaAPagar> listaQuery = AplicarStatus(baseQuery, filtro.Status, hoje);

        var contas = await listaQuery
            .Include(x => x.Fornecedor)
            .Include(x => x.PlanoContas)
            .OrderBy(x => x.DataVencimento)
            .ThenBy(x => x.Descricao)
            .ToListAsync(cancellationToken);

        filtro.Itens = contas.Select(x =>
        {
            decimal pago = x.ValorPago ?? 0;
            bool quitado = x.Status == "Pago";
            bool parcial = x.Status is "Pago Parcial" or "ParcialmentePago";
            bool cancelado = x.Status == "Cancelado";
            bool pendente = x.Status == "Pendente" && pago == 0;
            string statusExibicao = !cancelado && !quitado && !parcial && x.DataVencimento.Date < hoje
                ? "Vencido"
                : x.Status == "ParcialmentePago" ? "Pago Parcial" : x.Status;

            return new ContaAPagarListaItemViewModel
            {
                ContasAPagarId = x.ContasAPagarId,
                Descricao = x.Descricao,
                NumeroDocumento = x.NumeroDocumento,
                Fornecedor = x.Fornecedor?.Nome,
                Categoria = $"{x.PlanoContas.CodigoClassificacao} - {x.PlanoContas.Descricao}",
                DataVencimento = x.DataVencimento,
                Valor = x.Valor,
                ValorPago = pago,
                Status = statusExibicao,
                PodeEditar = pendente,
                PodeBaixar = !cancelado && !quitado && (pendente || parcial || x.Status == "Vencido"),
                PodeEstornar = quitado || parcial,
                PodeCancelar = pendente,
            };
        }).ToList();

        filtro.Categorias = await CarregarPlanosAsync(empresaId, cancellationToken);
        filtro.FormasPagamento = await db.FormasPagamento.AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.NomeFormaPagamento)
            .Select(x => new SelectListItem(x.NomeFormaPagamento, x.FormaPgtoId.ToString()))
            .ToListAsync(cancellationToken);

        return View(filtro);
    }

    [HttpPost("Baixar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Baixar(BaixarContaViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out int usuarioId))
            return Forbid();

        if (viewModel.DataPagamento.Date > DateTime.Today)
            ModelState.AddModelError(nameof(viewModel.DataPagamento), "A data do pagamento não pode ser futura.");

        bool formaValida = await db.FormasPagamento.AsNoTracking()
            .AnyAsync(x => x.FormaPgtoId == viewModel.FormaPgtoId && x.Ativo, cancellationToken);
        if (!formaValida)
            ModelState.AddModelError(nameof(viewModel.FormaPgtoId), "Selecione uma forma de pagamento válida.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = PrimeiroErroModelState();
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var conta = await db.ContasAPagar.SingleOrDefaultAsync(
            x => x.ContasAPagarId == viewModel.ContasAPagarId && x.EmpresaId == empresaId,
            cancellationToken);
        if (conta is null)
            return NotFound();

        if (conta.Status is "Pago" or "Cancelado")
        {
            TempData["Error"] = "Este título não está disponível para pagamento.";
            return RedirectToAction(nameof(Index));
        }

        decimal saldo = conta.Valor - (conta.ValorPago ?? 0);
        if (viewModel.ValorPago <= 0 || viewModel.ValorPago > saldo)
        {
            TempData["Error"] = $"Informe um pagamento entre R$ 0,01 e {saldo:C}.";
            return RedirectToAction(nameof(Index));
        }

        var caixa = await db.Caixas.SingleOrDefaultAsync(
            x => x.EmpresaId == empresaId && x.UsuarioAberturaId == usuarioId &&
                 x.DataCaixa == DateTime.Today && x.Status == "Aberto",
            cancellationToken);
        if (caixa is null)
        {
            TempData["Error"] = "Abra o caixa antes de pagar uma conta.";
            return RedirectToAction(nameof(Index));
        }

        conta.ValorPago = (conta.ValorPago ?? 0) + viewModel.ValorPago;
        bool quitada = conta.ValorPago >= conta.Valor - 0.01m;
        conta.Status = quitada ? "Pago" : "Pago Parcial";
        conta.DataPagamento = viewModel.DataPagamento.Date;
        conta.FormaPgtoId = viewModel.FormaPgtoId;
        conta.Observacoes = AnexarObservacao(conta.Observacoes, viewModel.Observacoes);

        db.CaixaMovimentos.Add(new CaixaMovimento
        {
            CaixaId = caixa.CaixaId,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            DataHora = DateTime.Now,
            Tipo = "SAIDA",
            Valor = viewModel.ValorPago,
            FormaPgtoId = viewModel.FormaPgtoId,
            Historico = Limitar($"Pagamento conta: {conta.Descricao}", 255),
            Origem = "ContasAPagar",
            ReferenciaId = conta.ContasAPagarId
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["Success"] = quitada ? "Conta paga com sucesso!" : "Pagamento parcial registrado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Estornar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Estornar(int id, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out int usuarioId))
            return Forbid();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var conta = await db.ContasAPagar.SingleOrDefaultAsync(
            x => x.ContasAPagarId == id && x.EmpresaId == empresaId,
            cancellationToken);
        if (conta is null)
            return NotFound();

        if (conta.Status is not ("Pago" or "Pago Parcial" or "ParcialmentePago"))
        {
            TempData["Error"] = "Somente títulos pagos podem ser estornados.";
            return RedirectToAction(nameof(Index));
        }

        var caixa = await db.Caixas.SingleOrDefaultAsync(
            x => x.EmpresaId == empresaId && x.UsuarioAberturaId == usuarioId &&
                 x.DataCaixa == DateTime.Today && x.Status == "Aberto",
            cancellationToken);
        if (caixa is null)
        {
            TempData["Error"] = "Abra o caixa antes de estornar um pagamento.";
            return RedirectToAction(nameof(Index));
        }

        var saidas = await db.CaixaMovimentos.Where(x =>
                x.EmpresaId == empresaId &&
                x.Origem == "ContasAPagar" &&
                x.ReferenciaId == id &&
                x.Tipo == "SAIDA")
            .ToListAsync(cancellationToken);

        foreach (var saida in saidas)
        {
            saida.Origem = "ContasAPagarEstornada";
            db.CaixaMovimentos.Add(new CaixaMovimento
            {
                CaixaId = caixa.CaixaId,
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                DataHora = DateTime.Now,
                Tipo = "ENTRADA",
                Valor = saida.Valor,
                FormaPgtoId = saida.FormaPgtoId,
                Historico = Limitar($"Estorno conta: {conta.Descricao}", 255),
                Origem = "EstornoContasAPagar",
                ReferenciaId = conta.ContasAPagarId
            });
        }

        conta.Status = "Pendente";
        conta.ValorPago = 0;
        conta.DataPagamento = null;
        conta.FormaPgtoId = null;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["Success"] = "Pagamento estornado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Cancelar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        var conta = await db.ContasAPagar.SingleOrDefaultAsync(
            x => x.ContasAPagarId == id && x.EmpresaId == empresaId,
            cancellationToken);
        if (conta is null)
            return NotFound();

        if (conta.Status != "Pendente" || (conta.ValorPago ?? 0) > 0)
        {
            TempData["Error"] = "Somente títulos pendentes e sem pagamentos podem ser cancelados.";
            return RedirectToAction(nameof(Index));
        }

        conta.Status = "Cancelado";
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Título cancelado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Editar/{id:int}")]
    public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        var conta = await db.ContasAPagar.AsNoTracking().SingleOrDefaultAsync(
            x => x.ContasAPagarId == id && x.EmpresaId == empresaId,
            cancellationToken);
        if (conta is null)
            return NotFound();

        if (conta.Status != "Pendente" || (conta.ValorPago ?? 0) > 0)
        {
            TempData["Error"] = "Somente títulos pendentes podem ser editados.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new EditarContaAPagarViewModel
        {
            ContasAPagarId = conta.ContasAPagarId,
            Descricao = conta.Descricao,
            NumeroDocumento = conta.NumeroDocumento,
            FornecedorId = conta.FornecedorId,
            PlanoContasId = conta.PlanoContasId,
            DataEmissao = conta.DataEmissao,
            DataVencimento = conta.DataVencimento,
            Valor = conta.Valor,
            Observacoes = conta.Observacoes
        };
        await CarregarOpcoesEdicaoAsync(viewModel, empresaId, cancellationToken);
        return View(viewModel);
    }

    [HttpPost("Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarContaAPagarViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        if (viewModel.DataVencimento.Date < viewModel.DataEmissao.Date)
            ModelState.AddModelError(nameof(viewModel.DataVencimento), "O vencimento não pode ser anterior à emissão.");

        if (viewModel.Valor <= 0)
            ModelState.AddModelError(nameof(viewModel.Valor), "O valor deve ser maior que zero.");

        if (viewModel.FornecedorId.HasValue && !await db.Fornecedores.AsNoTracking().AnyAsync(
                x => x.FornecedorId == viewModel.FornecedorId && x.EmpresaId == empresaId, cancellationToken))
            ModelState.AddModelError(nameof(viewModel.FornecedorId), "Selecione um fornecedor válido.");

        if (!await db.PlanosContas.AsNoTracking().AnyAsync(
                x => x.PlanoContasId == viewModel.PlanoContasId && x.EmpresaId == empresaId && x.Tipo == "D",
                cancellationToken))
            ModelState.AddModelError(nameof(viewModel.PlanoContasId), "Selecione uma conta de despesa válida.");

        if (!ModelState.IsValid)
        {
            await CarregarOpcoesEdicaoAsync(viewModel, empresaId, cancellationToken);
            return View(viewModel);
        }

        var conta = await db.ContasAPagar.SingleOrDefaultAsync(
            x => x.ContasAPagarId == viewModel.ContasAPagarId && x.EmpresaId == empresaId,
            cancellationToken);
        if (conta is null)
            return NotFound();

        if (conta.Status != "Pendente" || (conta.ValorPago ?? 0) > 0)
        {
            TempData["Error"] = "O título foi alterado e não está mais disponível para edição.";
            return RedirectToAction(nameof(Index));
        }

        conta.Descricao = viewModel.Descricao.Trim();
        conta.NumeroDocumento = NormalizarOpcional(viewModel.NumeroDocumento);
        conta.FornecedorId = viewModel.FornecedorId;
        conta.PlanoContasId = viewModel.PlanoContasId;
        conta.DataEmissao = viewModel.DataEmissao.Date;
        conta.DataVencimento = viewModel.DataVencimento.Date;
        conta.Valor = viewModel.Valor;
        conta.Observacoes = NormalizarOpcional(viewModel.Observacoes);

        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Conta a pagar atualizada com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Excluir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        var conta = await db.ContasAPagar.SingleOrDefaultAsync(
            x => x.ContasAPagarId == id && x.EmpresaId == empresaId,
            cancellationToken);
        if (conta is null)
            return NotFound();

        bool possuiMovimento = await db.CaixaMovimentos.AsNoTracking().AnyAsync(
            x => x.EmpresaId == empresaId && x.ReferenciaId == id &&
                 (x.Origem == "ContasAPagar" || x.Origem == "ContasAPagarEstornada"),
            cancellationToken);

        if (conta.Status != "Pendente" || (conta.ValorPago ?? 0) > 0 || possuiMovimento ||
            conta.PedidoId.HasValue || conta.DocumentoEntradaId.HasValue)
        {
            TempData["Error"] = "Somente títulos pendentes, manuais e sem movimentações podem ser excluídos.";
            return RedirectToAction(nameof(Index));
        }

        db.ContasAPagar.Remove(conta);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Conta a pagar excluída com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    private static IQueryable<ContaAPagar> AplicarStatus(
        IQueryable<ContaAPagar> query,
        string? status,
        DateTime hoje) => status switch
        {
            "Pendente" => query.Where(x => x.Status == "Pendente" || x.Status == "Pago Parcial" || x.Status == "ParcialmentePago"),
            "Pago" => query.Where(x => x.Status == "Pago"),
            "Vencido" => query.Where(x => (x.Status == "Pendente" || x.Status == "Vencido" || x.Status == "Pago Parcial" || x.Status == "ParcialmentePago") && x.DataVencimento.Date < hoje),
            "Cancelado" => query.Where(x => x.Status == "Cancelado"),
            _ => query
        };

    private async Task<IReadOnlyList<SelectListItem>> CarregarPlanosAsync(int empresaId, CancellationToken cancellationToken) =>
        await db.PlanosContas.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Tipo == "D")
            .OrderBy(x => x.CodigoClassificacao)
            .Select(x => new SelectListItem(x.CodigoClassificacao + " - " + x.Descricao, x.PlanoContasId.ToString()))
            .ToListAsync(cancellationToken);

    private async Task CarregarOpcoesEdicaoAsync(
        EditarContaAPagarViewModel viewModel,
        int empresaId,
        CancellationToken cancellationToken)
    {
        viewModel.Fornecedores = await db.Fornecedores.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.FornecedorId.ToString()))
            .ToListAsync(cancellationToken);
        viewModel.PlanosContas = await CarregarPlanosAsync(empresaId, cancellationToken);
    }

    private string PrimeiroErroModelState() => ModelState.Values
        .SelectMany(x => x.Errors)
        .Select(x => x.ErrorMessage)
        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Dados de pagamento inválidos.";

    private static string? AnexarObservacao(string? atual, string? nova)
    {
        nova = NormalizarOpcional(nova);
        if (nova is null) return atual;
        return string.IsNullOrWhiteSpace(atual) ? nova : $"{atual}{Environment.NewLine}{nova}";
    }

    private static string Limitar(string value, int tamanho) =>
        value.Length <= tamanho ? value : value[..tamanho];

    private static string? NormalizarOpcional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool TryGetContexto(out int empresaId, out int usuarioId)
    {
        bool empresaValida = int.TryParse(User.FindFirstValue("EmpresaID"), out empresaId) && empresaId > 0;
        bool usuarioValido = int.TryParse(User.FindFirstValue("UsuarioID"), out usuarioId) && usuarioId > 0;
        return empresaValida && usuarioValido;
    }
}
