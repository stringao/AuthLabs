using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthLabs.Windows.Services;

namespace AuthLabs.Windows.Controllers;

/// <summary>
/// Controller para endpoints de autenticacao Windows.
/// Fornece informacoes sobre o usuario autenticado e grupos do AD.
/// </summary>
/// <remarks>
/// JUNIOR: Controllers são classes que handling HTTP requests.
/// Cada metodo publico com [HttpGet], [HttpPost], etc. e um endpoint.
///
/// ROUTE DESTE CONTROLLER:
/// Todas as rotas começam com "/api/auth" porque o nome da classe e "AuthController"
/// e a rota base e "api/[controller]" = "api/auth".
///
/// ENDPOINTS DESTE CONTROLLER:
/// - GET /api/auth/me       - Informacoes do usuario atual (requer autenticacao)
/// - GET /api/auth/windows-login - Info sobre Windows Auth (publico)
/// - GET /api/auth/ad-groups - Grupos AD do usuario (requer autenticacao)
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IWindowsAuthService _windowsAuthService;

    /// <summary>
    /// Construtor do controller.
    /// </summary>
    /// <param name="windowsAuthService">
    /// Injetado pelo sistema de DI (Dependency Injection) do ASP.NET Core.
    /// </param>
    /// <remarks>
    /// JUNIOR: DI (Injecao de Dependencias) e um padrao onde o framework
    /// cria os objetos e os "injeta" no seu codigo.
    ///
    /// BENEFICIOS:
    /// - Nao precisa criar WindowsAuthService manualmente
    /// - Facilita testes (pode mockar o servico)
    /// - Permite trocar implementacoes sem mudar o controller
    ///
    /// O ASP.NET Core automaticamente injeta IWindowsAuthService porque
    /// registramos builder.Services.AddScoped&lt;IWindowsAuthService, WindowsAuthService&gt;()
    /// no Program.cs.
    /// </remarks>
    public AuthController(IWindowsAuthService windowsAuthService)
    {
        _windowsAuthService = windowsAuthService;
    }

    /// <summary>
    /// Obtem informacoes do usuario Windows atual.
    /// Requer autenticacao Windows.
    /// </summary>
    /// <returns>
    /// JSON com nome do usuario, tipo de autenticacao, se e admin, e roles.
    /// </returns>
    /// <response code="200">Retorna as informacoes do usuario autenticado.</response>
    /// <remarks>
    /// JUNIOR: [Authorize(AuthenticationSchemes = "Negotiate")]
    ///
    /// O atributo Authorize exige que o usuario esteja autenticado.
    /// AuthenticationSchemes = "Negotiate" especifica que deve usar
    /// Windows Authentication (protocolo Negotiate).
    ///
    /// O QUE ACONTECE QUANDO VOCE CHAMA ESTE ENDPOINT:
    /// 1. Browser envia request com票据 Kerberos ou header NTLM
    /// 2. Servidor valida票据/challenge
    /// 3. Se valido, ClaimsPrincipal e criado com Identity do Windows
    /// 4. WindowsClaimsTransformer adiciona claims de Role
    /// 5. Authorization valida que usuario esta autenticado
    /// 6. Este metodo executa e retorna info do usuario
    ///
    /// SE NAO AUTENTICADO:
    /// - Browser mostra dialog de login do Windows (no IE/Edge)
    /// - Ou usa credenciais do usuario logado automaticamente
    /// </remarks>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Negotiate")]
    public IActionResult GetCurrentUser()
    {
        var userName = _windowsAuthService.GetCurrentUserName(User);
        var authType = _windowsAuthService.GetAuthenticationType(User);
        var isAdmin = User.IsInRole("Admin");

        return Ok(new
        {
            userName,
            authType,
            isAdmin,
            roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value),
            message = "Usuario Windows autenticado"
        });
    }

    /// <summary>
    /// Retorna informacoes sobre a autenticacao Windows disponivel.
    /// Este endpoint e PUBLICO (nao requer autenticacao).
    /// </summary>
    /// <returns>
    /// JSON com informacoes sobre os esquemas de autenticacao suportados.
    /// </returns>
    /// <remarks>
    /// JUNIOR: [AllowAnonymous] permite acesso sem autenticacao.
    /// Isso e util para:
    /// - Páginas de login (que mostram opcoes de autenticacao)
    /// - Documentacao da API
    /// - Endpoints publicos de health check
    ///
    /// Schemas "Kerberos" e "NTLM":
    /// - Kerberos: Mais seguro, usa tickets, preferencial
    /// - NTLM: Fallback para clientes que nao suportam Kerberos
    ///
    /// NOTA: Ambos so funcionam no Windows. Em Linux/Mac, voce precisara
    /// de estrategias diferentes (LDAP direto, Identity Server, etc).
    /// </remarks>
    [HttpGet("windows-login")]
    [AllowAnonymous]
    public IActionResult GetWindowsLoginInfo()
    {
        return Ok(new
        {
            authenticationType = "Negotiate",
            schemes = new[] { "Kerberos", "NTLM" },
            description = "Windows Authentication usa Kerberos ou NTLM para autenticacao integrada com Active Directory",
            note = "Requer ambiente Windows (IIS ou Kestrel com Negotiate)"
        });
    }

    /// <summary>
    /// Obtem grupos do Active Directory do usuario atual.
    /// Requer autenticacao Windows.
    /// </summary>
    /// <returns>
    /// JSON com nome do usuario e lista de grupos do AD.
    /// </returns>
    /// <remarks>
    /// JUNIOR: Grupos do AD sao importantes para Authorization.
    ///
    /// EXEMPLO DE USO:
    /// Imagine que voce quer permitir acesso a uma area apenas para
    /// usuarios do grupo "Domain Admins" ou "Financeiros".
    /// Voce verificaria User.IsInRole("Domain Admins") ou
    /// chamaria IsInAdGroupAsync(User, "Domain Admins").
    ///
    /// DIFERENCA ENTRE Role E AD Group:
    /// - Role: Conceito da aplicacao (Admin, User, Manager)
    /// - AD Group: Conceito organizacional (Domain Admins, Financeiros, RH)
    ///
    /// O WindowsClaimsTransformer mapeia grupos AD para roles da aplicacao.
    /// </remarks>
    [HttpGet("ad-groups")]
    [Authorize(AuthenticationSchemes = "Negotiate")]
    public async Task<IActionResult> GetUserAdGroups()
    {
        var groups = await _windowsAuthService.GetUserAdGroupsAsync(User);
        return Ok(new
        {
            userName = _windowsAuthService.GetCurrentUserName(User),
            adGroups = groups
        });
    }
}
