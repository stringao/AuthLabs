using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Cookie.Controllers;

/// <summary>
/// Controller de autenticação usando Cookie Authentication.
///
/// DIFERENÇA ENTRE ESTE E JWT:
/// - JWT: Token é enviado no header "Authorization: Bearer <token>"
///   Cliente precisa guardar token e enviar manualmente
/// - Cookie: Token fica no cookie HTTP, navegador envia automaticamente
///   Mais fácil para apps web tradicionais
///
/// FLUXO DE AUTENTICAÇÃO POR COOKIE:
/// 1. POST /api/auth/login com email e senha
/// 2. Server valida credenciais
/// 3. Server cria "claims identity" com info do usuário
/// 4. Server cria cookie com serialized identity
/// 5. Browser guarda cookie
/// 6. Requests seguintes enviam cookie automaticamente
/// 7. Server lê cookie e identifica usuário
///
/// JUNIOR: Para o cliente, é mais simples - não precisa gerenciar token.
/// Mas para APIs que vão ser consumidas por mobile ou SPA,
/// JWT é geralmente melhor porque:
/// - Mobile apps não têm cookies naturais
/// - JWT é stateless (servidor não guarda sessão)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AuthController(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Login com email e senha.
    ///
    /// FLUXO:
    /// 1. Recebe email e senha
    /// 2. Chama SignInManager.PasswordSignInAsync para validar
    /// 3. Se válido, cria cookie de autenticação automaticamente
    /// 4. Retorna confirmação de sucesso
    ///
    /// JUNIOR: SignInManager é PROVIDED pelo ASP.NET Identity.
    /// Ele já sabe como:
    /// - Verificar se email existe
    /// - Fazer hash da senha e comparar
    /// - Criar o cookie de autenticação
    ///
    /// isPersistent: true = cookie persiste entre sessões do navegador
    /// (fechar navegador e abrir de novo, ainda está logado)
    /// lockoutOnFailure: false = não bloquear conta após falha
    /// (em produção, você talvez queira true após N tentativas)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // PasswordSignInAsync retorna SignInResult com propriedades:
        // - Succeeded: login funcionou
        // - IsLockedOut: conta bloqueada
        // - RequiresTwoFactor: precisa de 2FA
        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: true,   // Cookie persiste entre sessões
            lockoutOnFailure: false); // Não bloquear por falhas

        if (!result.Succeeded)
        {
            // Login falhou - retornar 401
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        return Ok(new { message = "Login realizado com sucesso" });
    }

    /// <summary>
    /// Logout - remove o cookie de autenticação.
    ///
    /// FLUXO:
    /// 1. Chama SignInManager.SignOutAsync()
    /// 2. Cookie é marcado como expirado e removido do navegador
    /// 3. Usuário não está mais autenticado
    ///
    /// JUNIOR: Diferente do JWT onde o token ainda seria válido,
    /// com Cookie Authentication o logout é REAL - servidor
    /// marca o cookie como inválido.
    /// (Na verdade, o servidor pode manter uma "blocklist" de cookies
    /// em produção para logout instantâneo real)
    /// </summary>
    [HttpPost("logout")]
    [Authorize] // Só usuários logados podem fazer logout
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logout realizado com sucesso" });
    }

    /// <summary>
    /// Retorna informações do usuário autenticado atual.
    ///
    /// FLUXO:
    /// 1. Obtém o usuário atual via UserManager
    /// 2. Obtém as roles do usuário
    /// 3. Retorna todos os dados
    ///
    /// JUNIOR: User é uma propriedade do ControllerBase.
    /// Ele representa a identidade do usuário extraída do cookie.
    ///
    /// _signInManager.UserManager é diferente de _userManager injetado?
    /// Na verdade, SignInManager TEM um UserManager interno.
    /// _signInManager.UserManager é o mesmo UserManager que você usaria
    /// diretamente. Pode usar qualquer um.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        // GetUserAsync obtém o usuário logado atual
        // Retorna null se não houver usuário (não deveria acontecer em endpoint [Authorize])
        var user = await _signInManager.UserManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { message = "Usuário não encontrado" });
        }

        // Obter roles do usuário
        var roles = await _signInManager.UserManager.GetRolesAsync(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            userName = user.UserName,
            roles = roles
        });
    }

    /// <summary>
    /// Endpoint chamado quando usuário tenta acessar recurso sem permissão.
    ///
    /// JUNIOR: Este endpoint é chamado AUTOMATICAMENTE pelo middleware
    /// de autorização quando:
    /// - Usuário está autenticado mas não tem a role necessária
    /// - Configuramos AccessDeniedPath no Program.cs
    ///
    /// Forbid() retorna HTTP 403 Forbidden:
    /// - 401 Unauthorized = "você não está logado"
    /// - 403 Forbidden = "você está logado, mas não tem permissão"
    /// </summary>
    [HttpGet("access-denied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        // Forbid() = retorna 403 Forbidden
        return Forbid();
    }
}

/// <summary>
/// DTO para request de login.
/// JUNIOR: DTO = Data Transfer Object
/// Objeto simples só para carregar dados entre cliente e servidor.
/// Não tem lógica de negócio, só dados.
/// </summary>
public record LoginRequest(string Email, string Password);