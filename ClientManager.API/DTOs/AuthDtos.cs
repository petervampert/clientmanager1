using System.ComponentModel.DataAnnotations;

namespace ClientManager.API.DTOs;

public record RegisterRequest(
    [Required, MinLength(3)] string Username,
    [Required, MinLength(6)] string Password
);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record AuthResponse(string Token, string Username, DateTime ExpiresAt);
