/*
 * IAuthenticationService - Interface wrapper for SignInManager operations
 * ======================================================================
 *
 * This interface wraps the SignInManager operations used by AuthController.
 * It exists to enable unit testing, since SignInManager<T> is a sealed class
 * that cannot be mocked directly with Moq.
 *
 * In production, the implementation delegates to the real SignInManager.
 * In tests, we mock this interface instead.
 */

using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthLabs.Rbac.Services;

public interface IAuthenticationService
{
    Task<SignInResult> PasswordSignInAsync(
        User user,
        string password,
        bool isPersistent,
        bool lockoutOnFailure);

    Task SignOutAsync();
}
