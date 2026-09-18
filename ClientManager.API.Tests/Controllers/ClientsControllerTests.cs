using ClientManager.API.Controllers;
using ClientManager.API.DTOs;
using ClientManager.API.Models;
using ClientManager.API.Services;
using ClientManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ClientManager.API.Tests.Controllers;

public class ClientsControllerTests
{
    private readonly Mock<IPdfReportService> _pdfMock;

    public ClientsControllerTests()
    {
        _pdfMock = new Mock<IPdfReportService>();
        _pdfMock.Setup(p => p.GenerateClientsReport(It.IsAny<IEnumerable<Client>>()))
                .Returns(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
    }

    private static Client MakeClient(int id = 1, string first = "Jane", string last = "Doe", string email = "jane@example.com") =>
        new() { Id = id, FirstName = first, LastName = last, Email = email, CreatedAt = DateTime.UtcNow };

    // ── GET /api/clients ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetClients_EmptyDb_ReturnsEmptyPagedResult()
    {
        using var db = DbContextFactory.Create();
        var controller = new ClientsController(db, _pdfMock.Object);

        var result = await controller.GetClients();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeOfType<PagedResult<ClientResponse>>().Subject;
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClients_ReturnsPaginatedResults()
    {
        using var db = DbContextFactory.Create();
        for (int i = 1; i <= 15; i++)
            db.Clients.Add(MakeClient(i, $"First{i}", $"Last{i}", $"user{i}@test.com"));
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var result = await controller.GetClients(page: 1, pageSize: 10);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeOfType<PagedResult<ClientResponse>>().Subject;
        paged.TotalCount.Should().Be(15);
        paged.Items.Should().HaveCount(10);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetClients_SearchByFirstName_ReturnsMatches()
    {
        using var db = DbContextFactory.Create();
        db.Clients.Add(MakeClient(1, "Alice", "Smith", "alice@test.com"));
        db.Clients.Add(MakeClient(2, "Bob", "Jones", "bob@test.com"));
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var result = await controller.GetClients(search: "alice");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeOfType<PagedResult<ClientResponse>>().Subject;
        paged.TotalCount.Should().Be(1);
        paged.Items.First().FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetClients_SearchByEmail_ReturnsMatches()
    {
        using var db = DbContextFactory.Create();
        db.Clients.Add(MakeClient(1, "Alice", "Smith", "alice@company.com"));
        db.Clients.Add(MakeClient(2, "Bob", "Jones", "bob@other.com"));
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var result = await controller.GetClients(search: "company.com");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeOfType<PagedResult<ClientResponse>>().Subject;
        paged.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetClients_PageBelowOne_ClampsToPageOne()
    {
        using var db = DbContextFactory.Create();
        db.Clients.Add(MakeClient(1));
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var result = await controller.GetClients(page: -5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeOfType<PagedResult<ClientResponse>>().Subject;
        paged.Page.Should().Be(1);
    }

    // ── GET /api/clients/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetClient_ExistingId_Returns200WithClient()
    {
        using var db = DbContextFactory.Create();
        var client = MakeClient(email: "unique@test.com");
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var result = await controller.GetClient(client.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ClientResponse>().Subject;
        response.Email.Should().Be("unique@test.com");
    }

    [Fact]
    public async Task GetClient_NonExistingId_Returns404()
    {
        using var db = DbContextFactory.Create();
        var controller = new ClientsController(db, _pdfMock.Object);

        var result = await controller.GetClient(999);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── POST /api/clients ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateClient_ValidRequest_Returns201WithClient()
    {
        using var db = DbContextFactory.Create();
        var controller = new ClientsController(db, _pdfMock.Object);
        var request = new ClientRequest("Jane", "Doe", "jane@test.com", null, null);

        var result = await controller.CreateClient(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<ClientResponse>().Subject;
        response.FirstName.Should().Be("Jane");
        response.Email.Should().Be("jane@test.com");
        response.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateClient_DuplicateEmail_Returns409()
    {
        using var db = DbContextFactory.Create();
        db.Clients.Add(MakeClient(email: "dupe@test.com"));
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var result = await controller.CreateClient(new ClientRequest("Other", "Person", "dupe@test.com", null, null));

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateClient_PersistsToDatabase()
    {
        using var db = DbContextFactory.Create();
        var controller = new ClientsController(db, _pdfMock.Object);

        await controller.CreateClient(new ClientRequest("Store", "Test", "store@test.com", null, null));

        db.Clients.Should().ContainSingle(c => c.Email == "store@test.com");
    }

    // ── PUT /api/clients/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateClient_ExistingClient_Returns200WithUpdatedData()
    {
        using var db = DbContextFactory.Create();
        var client = MakeClient(email: "old@test.com");
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var request = new ClientRequest("Updated", "Name", "new@test.com", "555-1234", "New Address");

        var result = await controller.UpdateClient(client.Id, request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ClientResponse>().Subject;
        response.FirstName.Should().Be("Updated");
        response.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task UpdateClient_NonExistingId_Returns404()
    {
        using var db = DbContextFactory.Create();
        var controller = new ClientsController(db, _pdfMock.Object);

        var result = await controller.UpdateClient(999, new ClientRequest("A", "B", "c@c.com", null, null));

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateClient_EmailTakenByAnotherClient_Returns409()
    {
        using var db = DbContextFactory.Create();
        db.Clients.Add(MakeClient(1, email: "first@test.com"));
        var second = MakeClient(2, email: "second@test.com");
        db.Clients.Add(second);
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        // try to change second client's email to first client's email
        var result = await controller.UpdateClient(second.Id, new ClientRequest("X", "Y", "first@test.com", null, null));

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task UpdateClient_SetsUpdatedAtTimestamp()
    {
        using var db = DbContextFactory.Create();
        var client = MakeClient(email: "ts@test.com");
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        await controller.UpdateClient(client.Id, new ClientRequest("A", "B", "ts@test.com", null, null));

        var updated = db.Clients.Find(client.Id);
        updated!.UpdatedAt.Should().NotBeNull();
        updated.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── DELETE /api/clients/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteClient_ExistingClient_Returns204()
    {
        using var db = DbContextFactory.Create();
        var client = MakeClient(email: "del@test.com");
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var result = await controller.DeleteClient(client.Id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteClient_RemovesFromDatabase()
    {
        using var db = DbContextFactory.Create();
        var client = MakeClient(email: "gone@test.com");
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        await controller.DeleteClient(client.Id);

        db.Clients.Should().NotContain(c => c.Email == "gone@test.com");
    }

    [Fact]
    public async Task DeleteClient_NonExistingId_Returns404()
    {
        using var db = DbContextFactory.Create();
        var controller = new ClientsController(db, _pdfMock.Object);

        var result = await controller.DeleteClient(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── GET /api/clients/report/pdf ───────────────────────────────────────────

    [Fact]
    public async Task GetPdfReport_Returns200WithPdfContentType()
    {
        using var db = DbContextFactory.Create();
        db.Clients.Add(MakeClient(email: "pdf@test.com"));
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        var result = await controller.GetPdfReport();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task GetPdfReport_CallsPdfService()
    {
        using var db = DbContextFactory.Create();
        db.Clients.Add(MakeClient(email: "call@test.com"));
        await db.SaveChangesAsync();

        var controller = new ClientsController(db, _pdfMock.Object);
        await controller.GetPdfReport();

        _pdfMock.Verify(p => p.GenerateClientsReport(It.IsAny<IEnumerable<Client>>()), Times.Once);
    }
}
