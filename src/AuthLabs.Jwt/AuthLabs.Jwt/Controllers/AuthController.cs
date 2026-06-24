using AuthLabs.Jwt.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Jwt.Controllers;

/// <summary>
/// Controller de autenticação - expõe os endpoints de login, refresh e logout.
///
/// ENDPOINTS:
/// - POST /api/auth/login  - Autentica usuário e retorna tokens
/// - POST /api/auth/refresh - Renova tokens usando refresh token
/// - POST /api/auth/logout  - Revoga refresh token (logout)
///
/// JUNIOR: Controllers são "pontos de entrada" da API.
/// Eles recebem requisições HTTP e delegam para serviços processarem.
/// Um bom controller NÃO tem lógica de negócio - só validação básica
/// e chamada de serviços.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Autentica usuário com email e senha.
    ///
    /// FLUXO:
    /// 1. Recebe email e senha do corpo da requisição
    /// 2. Chama AuthService.LoginAsync para validar
    /// 3. Se válido, retorna access token + refresh token
    /// 4. Se inválido, retorna 401 Unauthorized
    ///
    /// JUNIOR: [FromBody] significa que o JSON do corpo da requisição
    /// será deserializado para o tipo LoginRequest.
    /// [AllowAnonymous] permite que este endpoint seja chamado
    /// sem estar autenticado (natural, já que é o login!).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Chamar serviço de autenticação
        // JUNIOR: O "?" em "result?" é nullable - o serviço pode retornar
        // null se email/senha estiverem errados.
        var result = await _authService.LoginAsync(request.Email, request.Password);

        // Result null = autenticação falhou
        if (result == null)
        {
            // 401 = Unauthorized - credenciais inválidas
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        // Sucesso - retornar tokens
        // JUNIOR: Usamos anonymous type com { } para retornar JSON.
        // O C# converte automaticamente para JSON.
        //
        // expiresIn está em SEGUNDOS (900 = 15 minutos).
        // O cliente deve usar este valor para saber quando renovar o token.
        // NOTA: Este valor está HARDCODED como 900 - em produção,
        // deveria vir de jwtSettings.AccessTokenExpirationMinutes * 60
        return Ok(new
        {
            accessToken = result.Value.accessToken,
            refreshToken = result.Value.refreshToken,
            expiresIn = 900 // 15 minutes in seconds
        });
    }

    /// <summary>
    /// Renova tokens usando refresh token.
    ///
    /// FLUXO:
    /// 1. Recebe refresh token do corpo
    /// 2. Valida se token existe, não expirou, não foi revogado
    /// 3. Se válido, gera novo par de tokens
    /// 4. Revoga o refresh token antigo (rotação)
    /// 5. Retorna novos tokens
    ///
    /// JUNIOR: Refresh token é enviado no corpo da requisição,
    /// não no header Authorization! Isso porque o refresh typically
    /// é feito por um processo em background, não pelo usuário direto.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (result == null)
        {
            // Refresh token inválido, expirado ou revogado
            return Unauthorized(new { message = "Refresh token inválido ou expirado" });
        }

        return Ok(new
        {
            accessToken = result.Value.accessToken,
            refreshToken = result.Value.refreshToken,
            expiresIn = 900
        });
    }

    /// <summary>
    /// Logout - revoga o refresh token.
    ///
    /// FLUXO:
    /// 1. Recebe refresh token do corpo
    /// 2. Marca token como revogado no banco
    /// 3. Retorna confirmação
    ///
    /// JUNIOR: Por que revogar refresh token e não access token?
    /// - Access token expira em 15 minutos automaticamente
    /// - Refresh token tem vida mais longa (7 dias)
    /// - Revogar refresh token impede que alguém renove o acesso
    /// - Depois de 15 min, o access token expira naturalmente
    ///
    /// [Authorize] = este endpoint SÓ pode ser chamado por
    /// usuários autenticados. Se não tem token válido, retorna 401.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        // Revogar refresh token
        // JUNIOR: Não precisamos verificar o retorno - mesmo que o token
        // não exista, considerarmos o logout como "sucesso" para não
        // vazar informação sobre quais tokens existem no sistema.
        await _authService.RevokeRefreshTokenAsync(request.RefreshToken);

        return Ok(new { message = "Logout realizado com sucesso" });
    }
}

/// <summary>
/// DTO (Data Transfer Object) para request de login.
/// JUNIOR: DTOs são objetos simples só para transferir dados.
/// Não têm lógica, só propriedades. O nome Request/Response
/// indica que é dados de entrada de um endpoint.
/// </summary>
public record LoginRequest(string Email, string Password);

/// <summary>
/// DTO para request de refresh de token.
/// </summary>
public record RefreshRequest(string RefreshToken);

/// <summary>
/// DTO para request de logout.
/// </summary>
public record LogoutRequest(string RefreshToken);