namespace Glyloop.Application.DTOs.Account;

/// <summary>
/// Request DTO for user registration.
/// </summary>
public record RegisterRequestDto(
    string Email,
    string Password);

