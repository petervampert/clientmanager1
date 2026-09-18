using ClientManager.API.Models;
using ClientManager.API.Services;
using FluentAssertions;
using QuestPDF.Infrastructure;

namespace ClientManager.API.Tests.Services;

public class PdfReportServiceTests
{
    private readonly PdfReportService _sut;

    public PdfReportServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _sut = new PdfReportService();
    }

    [Fact]
    public void GenerateClientsReport_ReturnsNonEmptyByteArray()
    {
        var clients = new List<Client>
        {
            new() { Id = 1, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com", CreatedAt = DateTime.UtcNow }
        };

        var result = _sut.GenerateClientsReport(clients);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateClientsReport_StartsWithPdfMagicBytes()
    {
        var clients = new List<Client>
        {
            new() { Id = 1, FirstName = "John", LastName = "Smith", Email = "john@example.com", CreatedAt = DateTime.UtcNow }
        };

        var result = _sut.GenerateClientsReport(clients);

        // PDF files always begin with "%PDF"
        var header = System.Text.Encoding.ASCII.GetString(result, 0, 4);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GenerateClientsReport_WithEmptyList_StillReturnsPdf()
    {
        var result = _sut.GenerateClientsReport(new List<Client>());

        result.Should().NotBeNullOrEmpty();
        var header = System.Text.Encoding.ASCII.GetString(result, 0, 4);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GenerateClientsReport_WithMultipleClients_ReturnsPdf()
    {
        var clients = Enumerable.Range(1, 25).Select(i => new Client
        {
            Id = i,
            FirstName = $"First{i}",
            LastName = $"Last{i}",
            Email = $"user{i}@example.com",
            Phone = $"+1 555 {i:0000}",
            Address = $"{i} Main St",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var result = _sut.GenerateClientsReport(clients);

        result.Should().NotBeNullOrEmpty();
        var header = System.Text.Encoding.ASCII.GetString(result, 0, 4);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GenerateClientsReport_WithNullOptionalFields_DoesNotThrow()
    {
        var clients = new List<Client>
        {
            new() { Id = 1, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com", Phone = null, Address = null, CreatedAt = DateTime.UtcNow }
        };

        var act = () => _sut.GenerateClientsReport(clients);

        act.Should().NotThrow();
    }
}
