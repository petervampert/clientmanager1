using ClientManager.API.Data;
using ClientManager.API.DTOs;
using ClientManager.API.Models;
using ClientManager.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientManager.API.Controllers;

/// <summary>
/// Handles user registration and authentication, issuing JWT Bearer tokens.
/// All endpoints are public (no <c>[Authorize]</c> required).
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, ITokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Registers a new application user.
    /// </summary>
    /// <remarks>
    /// The password is hashed with BCrypt before storage and is never returned in any response.
    /// On success the response includes a ready-to-use JWT token so the client can proceed without a separate login step.
    /// </remarks>
    /// <param name="request">Registration credentials.</param>
    /// <returns>
    /// <c>201 Created</c> with an <see cref="AuthResponse"/> on success.<br/>
    /// <c>400 Bad Request</c> if validation fails.<br/>
    /// <c>409 Conflict</c> if the username is already taken.
    /// </returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var exists = await db.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
            return Conflict(new { message = "Username already taken." });

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (token, expiresAt) = tokenService.GenerateToken(user);
        return CreatedAtAction(nameof(Register), new AuthResponse(token, user.Username, expiresAt));
    }

    /// <summary>
    /// Authenticates an existing user with username and password.
    /// </summary>
    /// <remarks>
    /// Uses constant-time BCrypt verification to prevent timing-based username enumeration.
    /// Returns a generic error message on failure regardless of whether the username or password is wrong.
    /// </remarks>
    /// <param name="request">Login credentials.</param>
    /// <returns>
    /// <c>200 OK</c> with an <see cref="AuthResponse"/> on success.<br/>
    /// <c>400 Bad Request</c> if validation fails.<br/>
    /// <c>401 Unauthorized</c> if credentials are invalid.
    /// </returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var (token, expiresAt) = tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Username, expiresAt));
    }
}
