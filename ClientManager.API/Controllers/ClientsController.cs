using ClientManager.API.Data;
using ClientManager.API.DTOs;
using ClientManager.API.Models;
using ClientManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientManager.API.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController(AppDbContext db, IPdfReportService pdfService) : ControllerBase
{
    // GET /api/clients?page=1&pageSize=10&search=john
    [HttpGet]
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

    // GET /api/clients/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientResponse>> GetClient(int id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();
        return Ok(ToResponse(client));
    }

    // POST /api/clients
    [HttpPost]
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

    // PUT /api/clients/{id}
    [HttpPut("{id:int}")]
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

    // DELETE /api/clients/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();

        db.Clients.Remove(client);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/clients/report/pdf
    [HttpGet("report/pdf")]
    public async Task<IActionResult> GetPdfReport()
    {
        var clients = await db.Clients
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync();

        var pdf = pdfService.GenerateClientsReport(clients);

        return File(pdf, "application/pdf", $"clients-report-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    private static ClientResponse ToResponse(Client c) => new(
        c.Id, c.FirstName, c.LastName, c.Email,
        c.Phone, c.Address, c.CreatedAt, c.UpdatedAt
    );
}
