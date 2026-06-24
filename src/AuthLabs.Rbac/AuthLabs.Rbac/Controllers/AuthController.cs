/*
 * JUNIOR: AuthController - Controlador de Autenticação
 * ====================================================
 *
 * Este controller gerencia as operações de LOGIN e LOGOUT dos usuários.
 *
 * FLUXO DE AUTENTICAÇÃO COOKIE (como funciona):
 * 1. Usuário envia email/senha para /api/auth/login
 * 2. Server verifica as credenciais contra o banco de dados
 * 3. Se válido, o server cria um COOKIE de autenticação
 * 4. O browser armazena este cookie e o envia em TODAS as requisições seguintes
 * 5. Assim, o server "lembra" quem é o usuário em requests subsequentes
 *
 * DIFERENÇA COM JWT:
 * - Cookie: todo estado está no servidor (sessão)
 * - JWT: todo estado está no cliente (token assinado)
 */

using AuthLabs.Rbac.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Rbac.Controllers;

/// <summary>
/// Controller para operações de autenticação (login, logout).
/// Todos os métodos aqui lidam com a identificação do usuário.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly IAuthenticationService _authService;

    // JUNIOR: SignInManager é fornecido pelo Identity e gerencia:
    // - PasswordSignInAsync: verifica email + senha
    // - SignOutAsync: invalida o cookie
    // - IsSignedIn: verifica se o usuário atual está logado
    public AuthController(IRoleService roleService, IAuthenticationService authService)
    {
        _roleService = roleService;
        _authService = authService;
    }

    /// <summary>
    /// Realiza login com email e senha.
    /// Se bem-sucedido, cria um cookie de autenticação.
    /// </summary>
    /// <param name="request">Objeto com Email e Password do usuário</param>
    /// <returns>Informações do usuário logado e suas roles</returns>
    [HttpPost("login")]
    [AllowAnonymous] // JUNIOR: AllowAnonymous permite acesso mesmo sem estar logado (necessário para login!)
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // JUNIOR: Primeiro buscamos o usuário pelo email
        // Se não encontrar, as credenciais são inválidas (evitamos dar mais informações)
        var user = await _roleService.GetUserByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        // JUNIOR: PasswordSignInAsync verifica a senha
        // Parâmetros:
        // - isPersistent: true = cookie dura 24h, false = cookie expira ao fechar browser
        // - lockoutOnFailure: true = bloqueia usuário após falhas (desativado aqui)
        var result = await _authService.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        // JUNIOR: Se login funcionou, retornamos os dados do usuário e suas roles
        // Em apps reais, você também poderia retornar um token JWT aqui
        var roles = await _roleService.GetUserRolesAsync(user);
        return Ok(new
        {
            message = "Login realizado com sucesso",
            user = user.Email,
            roles
        });
    }

    /// <summary>
    /// Realiza logout do usuário atual.
    /// Invalidar o cookie de autenticação.
    /// </summary>
    /// <returns>Mensagem de sucesso</returns>
    [HttpPost("logout")]
    [Authorize] // JUNIOR: [Authorize] sem parâmetros = requer apenas estar logado
    public async Task<IActionResult> Logout()
    {
        // JUNIOR: SignOutAsync invalida o cookie - próximo request não terá autenticação
        await _authService.SignOutAsync();
        return Ok(new { message = "Logout realizado com sucesso" });
    }

    /// <summary>
    /// Retorna informações do usuário autenticado atual.
    /// Útil para verificar se o cookie ainda é válido.
    /// </summary>
    /// <returns>Email e roles do usuário logado</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        // JUNIOR: User.Identity.Name contém o identificador do usuário (neste caso, o email)
        // Isto vem do cookie de autenticação que foi validado pelo middleware
        var email = User.Identity?.Name;
        if (email == null)
        {
            return Unauthorized(new { message = "Não autenticado" });
        }

        var user = await _roleService.GetUserByEmailAsync(email);
        if (user == null)
        {
            return NotFound(new { message = "Usuário não encontrado" });
        }

        var roles = await _roleService.GetUserRolesAsync(user);
        return Ok(new
        {
            user = user.Email,
            roles
        });
    }
}

/// <summary>
/// JUNIOR: Record é como uma classe imutável (não pode ser alterada após criação)
/// Usado para representar dados de entrada (DTO - Data Transfer Object)
/// Este é o formato esperado no body do POST /login
/// </summary>
/// <param name="Email">Email do usuário para login</param>
/// <param name="Password">Senha do usuário</param>
public record LoginRequest(string Email, string Password);
