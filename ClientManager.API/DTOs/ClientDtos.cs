using System.ComponentModel.DataAnnotations;

namespace ClientManager.API.DTOs;

/// <summary>
/// Request body for creating or updating a client.
/// </summary>
/// <param name="FirstName">Client's first name. Required.</param>
/// <param name="LastName">Client's last name. Required.</param>
/// <param name="Email">Client's email address. Must be a valid email format and unique across all clients.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="Address">Optional mailing address.</param>
public record ClientRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    string? Phone,
    string? Address
);

/// <summary>
/// Read-only projection of a client returned by the API.
/// </summary>
/// <param name="Id">Auto-incremented primary key.</param>
/// <param name="FirstName">Client's first name.</param>
/// <param name="LastName">Client's last name.</param>
/// <param name="Email">Client's unique email address.</param>
/// <param name="Phone">Optional phone number.</param>
/// <param name="Address">Optional mailing address.</param>
/// <param name="CreatedAt">UTC timestamp when the client record was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the last update, or <c>null</c> if never updated.</param>
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

/// <summary>
/// Generic paginated result wrapper returned by list endpoints.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="TotalCount">Total number of items across all pages (before pagination).</param>
/// <param name="Page">The current 1-based page number.</param>
/// <param name="PageSize">The number of items requested per page.</param>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
