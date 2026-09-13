using System.ComponentModel.DataAnnotations;

namespace ClientManager.API.DTOs;

public record ClientRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    string? Phone,
    string? Address
);

public record ClientResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
