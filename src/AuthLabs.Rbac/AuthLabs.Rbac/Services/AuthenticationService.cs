/*
 * AuthenticationService - Implementation of IAuthenticationService
 * =================================================================
 *
 * This is a thin wrapper around SignInManager that implements IAuthenticationService.
 * It delegates all calls to the injected SignInManager instance.
 *
 * This class exists solely to enable unit testing of AuthController.
 * In production, it simply forwards calls to the real SignInManager.
 */

using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthLabs.Rbac.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly SignInManager<User> _signInManager;

    public AuthenticationService(SignInManager<User> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<SignInResult> PasswordSignInAsync(
        User user,
        string password,
        bool isPersistent,
        bool lockoutOnFailure)
    {
        return await _signInManager.PasswordSignInAsync(
            user,
            password,
            isPersistent,
            lockoutOnFailure);
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}
