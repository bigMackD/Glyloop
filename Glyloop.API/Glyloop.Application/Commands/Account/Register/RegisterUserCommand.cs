using Glyloop.Application.DTOs.Account;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Commands.Account.Register;

/// <summary>
/// Command to register a new user account.
/// Creates a new user with email and password using ASP.NET Core Identity.
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password) : IRequest<Result<UserRegisteredDto>>;

