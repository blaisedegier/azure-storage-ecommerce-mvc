using BasicEcom.Models;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BasicEcom.Services
{
    // This class is responsible for generating PDF documents using the QuestPDF library.
    /*
     * Code Attribution:
     * QuestPDF
     * MarcinZiabek
     * 28 August 2024
     * GitHub
     * https://github.com/QuestPDF/QuestPDF
     */
    public class PdfGeneratorService
    {
        /*
         * Code Attribution:
         * Getting Started
         * n.d.
         * QuestPDF
         * https://www.questpdf.com/getting-started.html
         */
        public byte[] GenerateInvoicePdf(Invoice invoice)
        {
            Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);

                    page.Header().Background(Colors.Blue.Lighten3).Padding(10)
                        .Text($"Invoice for {invoice.CustomerName}")
                        .FontSize(24).Bold().FontColor(Colors.Blue.Darken3);

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Spacing(5);

                        // Customer Information Section
                        column.Item().Background(Colors.Grey.Lighten4).Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Column(innerColumn =>
                            {
                                innerColumn.Spacing(3);
                                innerColumn.Item().Text($"Customer: {invoice.CustomerName}").FontSize(14).Bold();
                                innerColumn.Item().Text($"Email: {invoice.Email}").FontSize(12);
                                innerColumn.Item().Text($"Phone: {invoice.Phone}").FontSize(12);
                                innerColumn.Item().Text($"Address: {invoice.Address}").FontSize(12);
                                innerColumn.Item().Text($"Order Date: {invoice.OrderDate:MMMM dd, yyyy}").FontSize(12);
                            });

                        // Product Details Section
                        column.Item().PaddingTop(10).Text("Product Details:").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().Background(Colors.White).Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Product Name
                                columns.ConstantColumn(120); // Description
                                columns.ConstantColumn(80);  // Price
                                columns.ConstantColumn(60);  // Size
                                columns.ConstantColumn(60);  // Quantity
                                columns.ConstantColumn(80);  // Color
                            });

                            // Header Row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Product Name").FontSize(12).Bold().FontColor(Colors.White);
                                header.Cell().Element(CellStyle).Text("Description").FontSize(12).Bold().FontColor(Colors.White);
                                header.Cell().Element(CellStyle).Text("Price").FontSize(12).Bold().FontColor(Colors.White);
                                header.Cell().Element(CellStyle).Text("Size").FontSize(12).Bold().FontColor(Colors.White);
                                header.Cell().Element(CellStyle).Text("Quantity").FontSize(12).Bold().FontColor(Colors.White);
                                header.Cell().Element(CellStyle).Text("Color").FontSize(12).Bold().FontColor(Colors.White);
                            });

                            // Data Row
                            table.Cell().Element(CellStyle).Text(invoice.ProductName);
                            table.Cell().Element(CellStyle).Text(invoice.Description);
                            table.Cell().Element(CellStyle).AlignRight().Text(invoice.Price.ToString("C"));
                            table.Cell().Element(CellStyle).AlignCenter().Text(invoice.Size.ToString());
                            table.Cell().Element(CellStyle).AlignCenter().Text(invoice.Quantity.ToString());
                            table.Cell().Element(CellStyle).Text(invoice.Colour);
                        });

                        // Total Amount Section
                        column.Item().AlignRight().PaddingTop(10).Text($"Total: {invoice.TotalAmount:C}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                    });

                    page.Footer().AlignCenter().Padding(5)
                        .Text($"Generated on {DateTime.Now:MMMM dd, yyyy}").FontSize(10).Italic().FontColor(Colors.Grey.Darken1);
                });
            });

            return document.GeneratePdf();
        }

        private IContainer CellStyle(IContainer container)
        {
            return container.PaddingVertical(5)
                           .PaddingHorizontal(10)
                           .BorderBottom(1)
                           .BorderColor(Colors.Grey.Lighten2);
        }
    }
}