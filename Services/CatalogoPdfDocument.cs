using GVC.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GVC.Web.Services;

public sealed class CatalogoPdfDocument(CatalogoPdfViewModel model) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(10).FontColor(Colors.Grey.Darken4));
            page.Header().Element(Cabecalho);
            page.Content().PaddingVertical(18).Element(Conteudo);
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span($"{model.RazaoSocial}  •  Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        });
    }

    private void Cabecalho(IContainer container)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(12).Row(row =>
        {
            if (model.Logo is not null)
                row.ConstantItem(90).Height(58).Image(model.Logo).FitArea();

            row.RelativeItem().PaddingLeft(12).Column(column =>
            {
                column.Item().Text("CATÁLOGO DE PRODUTOS").FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                column.Item().Text(model.EmpresaNome).FontSize(14).SemiBold();
                column.Item().Text(model.Contato);
                column.Item().Text(model.Endereco).FontSize(9).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(90).AlignRight().Text($"Atualizado em\n{model.AtualizadoEm:dd/MM/yyyy}").FontSize(9);
        });
    }

    private void Conteudo(IContainer container)
    {
        if (model.Produtos.Count == 0)
        {
            container.AlignCenter().AlignMiddle().Text("Nenhum produto disponível para este filtro.").FontSize(14);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            foreach (ItemCatalogoPdfViewModel produto in model.Produtos)
            {
                table.Cell().Padding(5).Element(cell => CardProduto(cell, produto));
            }
        });
    }

    private static void CardProduto(IContainer container, ItemCatalogoPdfViewModel produto)
    {
        container.MinHeight(205).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White)
            .Padding(8).Column(column =>
            {
                column.Item().Height(105).Background(Colors.Grey.Lighten4).Element(image =>
                {
                    if (produto.Imagem is not null) image.Image(produto.Imagem).FitArea();
                    else image.AlignCenter().AlignMiddle().Text("SEM FOTO").FontColor(Colors.Grey.Medium);
                });
                if (produto.EmPromocao)
                    column.Item().PaddingTop(5).Text("PROMOÇÃO").FontSize(8).Bold().FontColor(Colors.Red.Medium);
                column.Item().PaddingTop(4).Text(produto.Nome).Bold().FontSize(11);
                column.Item().Text(produto.Marca).FontSize(8).FontColor(Colors.Grey.Darken1);
                if (produto.EmPromocao)
                    column.Item().Text($"De {produto.PrecoOriginal:C}").FontSize(8).Strikethrough().FontColor(Colors.Grey.Medium);
                column.Item().Text(produto.Preco.ToString("C")).FontSize(15).Bold().FontColor(Colors.Green.Darken2);
            });
    }
}
