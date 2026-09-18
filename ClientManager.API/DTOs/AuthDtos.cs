using System.ComponentModel.DataAnnotations;

namespace ClientManager.API.DTOs;

/// <summary>
/// Request body for registering a new application user.
/// </summary>
/// <param name="Username">Unique username. Minimum 3 characters.</param>
/// <param name="Password">Plain-text password (hashed with BCrypt before storage). Minimum 6 characters.</param>
public record RegisterRequest(
    [Required, MinLength(3)] string Username,
    [Required, MinLength(6)] string Password
);

/// <summary>
/// Request body for authenticating an existing user.
/// </summary>
/// <param name="Username">The registered username.</param>
/// <param name="Password">The plain-text password to verify against the stored BCrypt hash.</param>
public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

/// <summary>
/// Returned after a successful login or registration.
/// Store <see cref="Token"/> and send it as <c>Authorization: Bearer &lt;token&gt;</c> on subsequent requests.
/// </summary>
/// <param name="Token">Signed JWT Bearer token.</param>
/// <param name="Username">The authenticated user's username.</param>
/// <param name="ExpiresAt">UTC timestamp when the token expires (default 8 hours).</param>
public record AuthResponse(string Token, string Username, DateTime ExpiresAt);
