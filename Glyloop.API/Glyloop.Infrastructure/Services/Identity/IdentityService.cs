using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using Glyloop.Domain.ValueObjects;
using Glyloop.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Glyloop.Infrastructure.Services.Identity;

/// <summary>
/// Implementation of IIdentityService using ASP.NET Core Identity.
/// Handles user registration, authentication, and preference management.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<Result<Guid>> CreateUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return Result.Failure<Guid>(
                Error.Create("User.EmailAlreadyExists", "A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true, // MVP: no email verification
            TirLowerBound = 70,    // Default TIR range
            TirUpperBound = 180
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<Guid>(
                Error.Create("User.CreationFailed", errors));
        }

        return Result.Success(user.Id);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is not null;
    }

    public async Task<Result<TirRange>> GetUserPreferencesAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Result.Failure<TirRange>(
                Error.Create("User.NotFound", "User not found."));
        }

        var tirRange = TirRange.Create(user.TirLowerBound, user.TirUpperBound);
        return tirRange;
    }

    public async Task<Result> UpdateUserPreferencesAsync(
        UserId userId,
        TirRange tirRange,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Result.Failure(
                Error.Create("User.NotFound", "User not found."));
        }

        user.TirLowerBound = tirRange.Lower;
        user.TirUpperBound = tirRange.Upper;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(
                Error.Create("User.UpdateFailed", errors));
        }

        return Result.Success();
    }

    public async Task<Result<(Guid UserId, string Email)>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure<(Guid, string)>(
                Error.Create("Auth.InvalidCredentials", "Invalid email or password."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return Result.Failure<(Guid, string)>(
                    Error.Create("Auth.AccountLockedOut", "Account is locked due to multiple failed login attempts."));
            }

            return Result.Failure<(Guid, string)>(
                Error.Create("Auth.InvalidCredentials", "Invalid email or password."));
        }

        return Result.Success((user.Id, user.Email!));
    }
}

