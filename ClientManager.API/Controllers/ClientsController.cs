using ClientManager.API.Data;
using ClientManager.API.DTOs;
using ClientManager.API.Models;
using ClientManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientManager.API.Controllers;

/// <summary>
/// Provides CRUD operations for client records and PDF report generation.
/// All endpoints require a valid JWT Bearer token (<c>Authorization: Bearer &lt;token&gt;</c>).
/// </summary>
[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController(AppDbContext db, IPdfReportService pdfService) : ControllerBase
{
    /// <summary>
    /// Returns a paginated, optionally filtered list of clients ordered by last name then first name.
    /// </summary>
    /// <param name="page">1-based page number. Values below 1 are clamped to 1.</param>
    /// <param name="pageSize">Number of items per page (1–100). Values outside range default to 10.</param>
    /// <param name="search">
    /// Optional search term. Case-insensitive substring match across
    /// <c>FirstName</c>, <c>LastName</c>, <c>Email</c>, and <c>Phone</c>.
    /// </param>
    /// <returns><c>200 OK</c> with a <see cref="PagedResult{ClientResponse}"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClientResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClientResponse>>> GetClients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = db.Clients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                (c.Phone != null && c.Phone.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToResponse(c))
            .ToListAsync();

        return Ok(new PagedResult<ClientResponse>(items, totalCount, page, pageSize));
    }

    /// <summary>
    /// Returns a single client by their ID.
    /// </summary>
    /// <param name="id">The client's primary key.</param>
    /// <returns>
    /// <c>200 OK</c> with a <see cref="ClientResponse"/>.<br/>
    /// <c>404 Not Found</c> if no client with the given ID exists.
    /// </returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientResponse>> GetClient(int id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();
        return Ok(ToResponse(client));
    }

    /// <summary>
    /// Creates a new client record.
    /// </summary>
    /// <param name="request">Client details. Email must be unique across all clients.</param>
    /// <returns>
    /// <c>201 Created</c> with the new <see cref="ClientResponse"/> and a <c>Location</c> header.<br/>
    /// <c>400 Bad Request</c> if validation fails.<br/>
    /// <c>409 Conflict</c> if the email address is already in use.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientResponse>> CreateClient([FromBody] ClientRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var emailTaken = await db.Clients.AnyAsync(c => c.Email == request.Email);
        if (emailTaken) return Conflict(new { message = "A client with this email already exists." });

        var client = new Client
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, ToResponse(client));
    }

    /// <summary>
    /// Updates all fields of an existing client.
    /// </summary>
    /// <param name="id">The client's primary key.</param>
    /// <param name="request">Updated client details. Email must be unique (excluding the current client).</param>
    /// <returns>
    /// <c>200 OK</c> with the updated <see cref="ClientResponse"/>.<br/>
    /// <c>400 Bad Request</c> if validation fails.<br/>
    /// <c>404 Not Found</c> if no client with the given ID exists.<br/>
    /// <c>409 Conflict</c> if the email address is already used by a different client.
    /// </returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] ClientRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();

        var emailTaken = await db.Clients.AnyAsync(c => c.Email == request.Email && c.Id != id);
        if (emailTaken) return Conflict(new { message = "Another client already uses this email." });

        client.FirstName = request.FirstName;
        client.LastName = request.LastName;
        client.Email = request.Email;
        client.Phone = request.Phone;
        client.Address = request.Address;
        client.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToResponse(client));
    }

    /// <summary>
    /// Permanently deletes a client record.
    /// </summary>
    /// <param name="id">The client's primary key.</param>
    /// <returns>
    /// <c>204 No Content</c> on success.<br/>
    /// <c>404 Not Found</c> if no client with the given ID exists.
    /// </returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();

        db.Clients.Remove(client);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Generates and streams a PDF report of all clients, sorted alphabetically by last name.
    /// </summary>
    /// <returns>
    /// <c>200 OK</c> with <c>Content-Type: application/pdf</c> and the filename
    /// <c>clients-report-{yyyyMMdd}.pdf</c>.
    /// </returns>
    [HttpGet("report/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPdfReport()
    {
        var clients = await db.Clients
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync();

        var pdf = pdfService.GenerateClientsReport(clients);

        return File(pdf, "application/pdf", $"clients-report-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    /// <summary>Maps a <see cref="Client"/> entity to a <see cref="ClientResponse"/> DTO.</summary>
    private static ClientResponse ToResponse(Client c) => new(
        c.Id, c.FirstName, c.LastName, c.Email,
        c.Phone, c.Address, c.CreatedAt, c.UpdatedAt
    );
}
