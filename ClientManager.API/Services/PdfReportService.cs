using ClientManager.API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClientManager.API.Services;

/// <summary>
/// Generates PDF reports for client data using QuestPDF (Community license).
/// </summary>
public interface IPdfReportService
{
    /// <summary>
    /// Generates an A4 PDF report listing all provided clients in a formatted table.
    /// </summary>
    /// <param name="clients">The collection of clients to include in the report. Typically pre-sorted by the caller.</param>
    /// <returns>A <c>byte[]</c> containing the raw PDF document, ready to stream as <c>application/pdf</c>.</returns>
    byte[] GenerateClientsReport(IEnumerable<Client> clients);
}

/// <inheritdoc />
public class PdfReportService : IPdfReportService
{
    /// <inheritdoc />
    /// <remarks>
    /// Layout: A4 page, 2 cm margins.<br/>
    /// - <b>Header:</b> "Client Report" title + UTC generation timestamp.<br/>
    /// - <b>Content:</b> Table with columns: #, Name, Email, Phone, Address, Registered.
    ///   Header row has a dark-blue background; data rows alternate white / light grey.<br/>
    /// - <b>Footer:</b> "Page X of Y".<br/>
    /// - <b>Summary:</b> Right-aligned total client count below the table.
    /// </remarks>
    public byte[] GenerateClientsReport(IEnumerable<Client> clients)
    {
        var clientList = clients.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, clientList));

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>Renders the page header with title and generation timestamp.</summary>
    private static void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item()
                   .Text("Client Report")
                   .FontSize(20)
                   .Bold()
                   .FontColor(Colors.Blue.Darken2);

                col.Item()
                   .Text($"Generated on {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
                   .FontSize(9)
                   .FontColor(Colors.Grey.Darken1);
            });
        });
    }

    /// <summary>Renders the client data table and total count summary.</summary>
    private static void ComposeContent(IContainer container, List<Client> clients)
    {
        container.Column(col =>
        {
            // ── Data table ────────────────────────────────────────────────
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // #
                    columns.RelativeColumn(2);    // Name
                    columns.RelativeColumn(3);    // Email
                    columns.RelativeColumn(2);    // Phone
                    columns.RelativeColumn(3);    // Address
                    columns.RelativeColumn(2);    // Registered
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("#");
                    header.Cell().Element(HeaderCell).Text("Name");
                    header.Cell().Element(HeaderCell).Text("Email");
                    header.Cell().Element(HeaderCell).Text("Phone");
                    header.Cell().Element(HeaderCell).Text("Address");
                    header.Cell().Element(HeaderCell).Text("Registered");

                    static IContainer HeaderCell(IContainer c) =>
                        c.DefaultTextStyle(x => x.Bold())
                         .Background(Colors.Blue.Darken2)
                         .Padding(5)
                         .AlignMiddle();
                });

                foreach (var (client, index) in clients.Select((c, i) => (c, i)))
                {
                    var bg = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;

                    table.Cell().Element(c => DataCell(c, bg)).Text((index + 1).ToString());
                    table.Cell().Element(c => DataCell(c, bg)).Text($"{client.FirstName} {client.LastName}");
                    table.Cell().Element(c => DataCell(c, bg)).Text(client.Email);
                    table.Cell().Element(c => DataCell(c, bg)).Text(client.Phone ?? "-");
                    table.Cell().Element(c => DataCell(c, bg)).Text(client.Address ?? "-");
                    table.Cell().Element(c => DataCell(c, bg)).Text(client.CreatedAt.ToString("dd/MM/yyyy"));
                }

                static IContainer DataCell(IContainer c, string bg) =>
                    c.Background(bg).Padding(5).AlignMiddle();
            });

            // ── Summary ───────────────────────────────────────────────────
            col.Item()
               .PaddingTop(10)
               .AlignRight()
               .Text($"Total clients: {clients.Count}")
               .Bold();
        });
    }
}
