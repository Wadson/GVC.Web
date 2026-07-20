# Execute na raiz do projeto para criar, sem sobrescrever, os arquivos complementares do ERP.
$paths = @(
  'Pages/Shared/_Layout.cshtml.cs','Pages/Shared/_CookieConsentPartial.cshtml',
  'Pages/Account/ForgotPassword.cshtml','Pages/Account/ForgotPassword.cshtml.cs','Pages/Account/ResetPassword.cshtml','Pages/Account/ResetPassword.cshtml.cs','Pages/Account/AccessDenied.cshtml',
  'Pages/Usuarios/Index.cshtml','Pages/Usuarios/Index.cshtml.cs','Pages/Usuarios/Create.cshtml','Pages/Usuarios/Create.cshtml.cs','Pages/Usuarios/Edit.cshtml','Pages/Usuarios/Edit.cshtml.cs','Pages/Usuarios/Details.cshtml','Pages/Usuarios/Delete.cshtml',
  'Pages/Clientes/Edit.cshtml','Pages/Clientes/Edit.cshtml.cs','Pages/Clientes/Details.cshtml',
  'Pages/Fornecedores/Index.cshtml','Pages/Fornecedores/Index.cshtml.cs','Pages/Fornecedores/Create.cshtml','Pages/Fornecedores/Edit.cshtml',
  'Pages/Vendedores/Index.cshtml','Pages/Vendedores/Create.cshtml','Pages/Vendedores/Edit.cshtml',
  'Pages/Produtos/Index.cshtml','Pages/Produtos/Create.cshtml','Pages/Produtos/Edit.cshtml','Pages/Produtos/Details.cshtml',
  'Pages/Estoque/Movimentacao.cshtml','Pages/Estoque/Extrato.cshtml','Pages/Estoque/AjusteManual.cshtml',
  'Pages/Vendas/Index.cshtml','Pages/Vendas/Details.cshtml','Pages/Vendas/Cancelar.cshtml',
  'Pages/Caixa/Abertura.cshtml','Pages/Caixa/Abertura.cshtml.cs','Pages/Caixa/Fechamento.cshtml','Pages/Caixa/Fechamento.cshtml.cs','Pages/Caixa/Index.cshtml','Pages/Caixa/LancarMovimento.cshtml','Pages/Caixa/Fluxo.cshtml',
  'Pages/Financeiro/Recebiveis/Index.cshtml','Pages/Financeiro/Recebiveis/Receber.cshtml','Pages/Financeiro/Recebiveis/Historico.cshtml',
  'Pages/Financeiro/ContasPagar/Index.cshtml','Pages/Financeiro/ContasPagar/Create.cshtml','Pages/Financeiro/ContasPagar/Pagar.cshtml',
  'Pages/Financeiro/Comissoes/Index.cshtml','Pages/Financeiro/Comissoes/Gerar.cshtml','Pages/Financeiro/Comissoes/Pagar.cshtml',
  'Pages/Configuracoes/Empresa/Index.cshtml','Pages/Configuracoes/Empresa/Edit.cshtml','Pages/Configuracoes/Pix/Configurar.cshtml','Pages/Configuracoes/FormasPagamento/Index.cshtml','Pages/Configuracoes/FormasPagamento/Create.cshtml',
  'Pages/Fiscal/NotasFiscais/Index.cshtml','Pages/Fiscal/NotasFiscais/Emitir.cshtml','Pages/Fiscal/NotasFiscais/Consultar.cshtml','Pages/Fiscal/Certificado/Configurar.cshtml',
  'Pages/Relatorios/Vendas.cshtml','Pages/Relatorios/Estoque.cshtml','Pages/Relatorios/Financeiro.cshtml','Pages/Relatorios/Fiscal.cshtml',
  'wwwroot/js/dashboard.js','wwwroot/images/.gitkeep'
)
foreach ($relativePath in $paths) {
  $absolutePath = Join-Path $PSScriptRoot $relativePath
  $directory = Split-Path $absolutePath -Parent
  if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
  if (-not (Test-Path -LiteralPath $absolutePath)) { New-Item -ItemType File -Path $absolutePath | Out-Null }
}
Write-Host "Estrutura complementar criada sem sobrescrever arquivos existentes."
