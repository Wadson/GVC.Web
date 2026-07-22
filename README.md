# GVC.Web - Sistema ERP

**GVC.Web** é uma solução de Gestão de Vendas e Comercialização (ERP) baseada em web, desenvolvida com **ASP.NET Core Razor Pages** e **Entity Framework Core**. O sistema foi desenhado para suportar operação multiempresa, controle financeiro, gestão de estoque, caixa, ponto de venda (PDV) e relatórios gerenciais.

---

## 🛠️ Tecnologias e Dependências

* **Framework:** ASP.NET Core 8.0 (Razor Pages)
* **ORM:** Entity Framework Core 8.0.19 (SQL Server)
* **Front-end & UI:** Bootstrap, jQuery, jQuery Validation & Unobtrusive
* **Arquitetura:** Multiempresa (segregação por `EmpresaId`), Padrão PageModels com Construtores Primários

---

## 📂 Estrutura do Projeto e Módulos

```text
GVC.Web/
├── Pages/
│   ├── Caixa/                # Controle de situação, abertura e fechamento de caixa
│   ├── Configuracoes/        # Restrito a administradores (Empresas, Formas de Pagamento)
│   ├── Estoque/              # Extrato de movimentações e Ajustes Manuais
│   ├── Financeiro/           # Contas a Pagar e Recebíveis (Recebimento total/parcial e histórico)
│   ├── Fiscal/               # Consulta de documentos e notas fiscais
│   ├── Relatorios/           # Relatório de vendas e consolidados
│   └── Shared/               # Layouts (_Layout.cshtml, _ViewStart.cshtml, etc.)
├── wwwroot/
│   ├── css/                  # Estilos globais (site.css) e específicos (erp.css)
│   ├── js/                   # Scripts comuns (site.js) e lógica do PDV (pdv.js)
│   └── uploads/produtos/     # Armazenamento de imagens do catálogo
├── appsettings.json          # Strings de conexão, logs e e-mail
└── scaffold-erp.ps1          # Script de criação/scaffolding inicial do projeto
```

---

## 🔄 Fluxos Principais do Sistema

1. **Autenticação & Multiempresa:**
   * O login valida usuário/senha e emite o cookie de sessão com as alegações (*Claims*), incluindo `EmpresaID` e `UsuarioID`.
   * As páginas herdam de `BasePageModel`, garantindo a aplicação automática do filtro de `EmpresaId`.

2. **Venda / Ponto de Venda (PDV):**
   * Busca no catálogo -> Carrinho (`pdv.js`) -> Seleção de cliente, vendedor e forma de pagamento -> Geração de parcelas.
   * O serviço `VendaService` processa a transação no banco de dados: registra vendas e itens, realiza a baixa em estoque (`MovimentacoesEstoque`), gera parcelas e efetua a entrada no Caixa.

3. **Contas a Receber & Financeiro:**
   * Listagem de recebíveis filtrada por padrão (*Pendente*, *Atrasada*, *ParcialmentePago*).
   * Registro de recebimentos parciais/totais com histórico completo e suporte a estorno.

4. **Controle de Estoque:**
   * Manutenção do saldo atualizado no cadastro de produtos.
   * Registros automáticos de `SAIDA` por vendas e movimentações avulsas via `AjusteManual`.

---

## 🚀 Como Executar o Projeto

1. **Pré-requisitos:**
   * .NET 8.0 SDK
   * Instância do SQL Server

2. **Configuração da String de Conexão:**
   Ajuste o arquivo `appsettings.json` ou `appsettings.Development.json` com suas credenciais do banco de dados SQL Server:
   ```json
   {
     "ConnectionStrings": {
       "ErpConnection": "Server=SEU_SERVIDOR;Database=GVC_DB;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Execução:**
   Execute via linha de comando ou pela sua IDE de preferência (Visual Studio / VS Code / JetBrains Rider):
   ```bash
   dotnet run --project GVC.Web.csproj
   ```

---

## ⚖️ Licença e Direitos Autorais

Desenvolvido por **WR Soft Serviços OnLine**  
Copyright © 2026 **WR Soft Serviços OnLine**. Todos os direitos reservados.
