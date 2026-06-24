using System.Security.Claims;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.OAuth.Controllers;

/// <summary>
/// Controller para autenticação usando OAuth 2.0 + OpenID Connect (OIDC).
/// Gerencia login, logout e informações do usuário.
/// </summary>
/// <remarks>
/// JUNIOR: O que é AuthController?
/// Este controller é o "porteiro" da autenticação. Ele:
///
/// 1. LOGIN (/api/auth/login/{provider}):
///    - Inicia o fluxo OAuth redirecionando para o provider
///    - O usuário faz login no provider (Google, GitHub, etc)
///    - O provider redireciona de volta para o Callback
///
/// 2. CALLBACK (/api/auth/callback/{provider}):
///    - Recebe o usuário de volta do provider
///    - Valida as informações e cria/loga o usuário na app
///    - Vincula o login externo ao usuário local
///
/// 3. LOGOUT (/api/auth/logout):
///    - Encerra a sessão do usuário na aplicação
///
/// 4. ME (/api/auth/me):
///    - Retorna informações do usuário autenticado
///
/// -----------------------------------------------------------
/// AUTENTICAÇÃO vs AUTORIZAÇÃO - Qual a diferença?
/// -----------------------------------------------------------
///
/// AUTENTICAÇÃO (Authentication - "Quem é você?")
/// - Verifica a identidade do usuário
/// - "Você é mesmo o João?"
/// - No OAuth: acontece no provider (Google pergunta "faça login")
///
/// AUTORIZAÇÃO (Authorization - "O que você pode fazer?")
/// - Verifica as permissões do usuário
/// - "João pode acessar dados de Admins?"
/// - No OAuth: acontece na SUA aplicação após autenticação
///
/// Resumindo:
/// - Autenticação = provar quem você é
/// - Autorização = verificar o que você tem permissão para fazer
/// -----------------------------------------------------------
///
/// OAuth 2.0 + OIDC - Fluxo Authorization Code (mais seguro)
/// -----------------------------------------------------------
///
/// Este projeto usa o "Authorization Code Flow" que é o mais seguro:
///
/// 1. [USUÁRIO] Clica em "Login com Google"
/// 2. [APP] Redireciona para Google com parâmetros:
///    - client_id, redirect_uri, scope, state, response_type=code
/// 3. [GOOGLE] Mostra tela de login e permissão ao usuário
/// 4. [USUÁRIO] Faz login e clica "Permitir"
/// 5. [GOOGLE] Redireciona para APP com ?code=AUTH_CODE
/// 6. [APP] Recebe o código (NÃO é ainda o token!)
/// 7. [APP] Faz requisição POST ao Token Endpoint do Google:
///    - Envia: code, client_id, client_secret, redirect_uri
/// 8. [GOOGLE] Retorna: access_token, id_token, refresh_token
/// 9. [APP] Usa access_token para chamar UserInfo e obter dados
/// 10. [APP] Cria/atualiza usuário local e faz login
///
/// Por que não usar o Implicit Flow?
/// - Implicit Flow envia tokens na URL (exposto em logs, histórico)
/// - Authorization Code é mais seguro: tokens só no backend
/// -----------------------------------------------------------
///
/// OIDC (OpenID Connect) é uma camada sobre OAuth 2.0
/// - OAuth 2.0: apenas autorização (acesso a recursos)
/// - OIDC: adiciona autenticação (saber "quem é")
/// - OIDC fornece o id_token (JWT com dados do usuário)
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    /// <summary>
    /// Inicializa o controller com as dependências do Identity.
    /// </summary>
    /// <param name="signInManager">Gerencia operações de login/logout do Identity.</param>
    /// <param name="userManager">Gerencia criação e busca de usuários.</param>
    /// <remarks>
    /// JUNIOR: Injeção de Dependência
    /// O ASP.NET Core injeta automaticamente SignInManager e UserManager.
    /// Estes são serviços do ASP.NET Identity que já vêm configurados.
    /// </remarks>
    public AuthController(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Inicia o fluxo de login OAuth com o provider especificado.
    /// Redireciona o usuário para o provider (Google, GitHub, etc).
    /// </summary>
    /// <param name="provider">Nome do provider OAuth (ex: "Google", "GitHub"). Deve corresponder a um provider configurado.</param>
    /// <param name="returnUrl">URL para redirecionamento após login bem-sucedido. Default é "/" (raiz).</param>
    /// <returns>Redirecionamento para o provider de autenticação.</returns>
    /// <remarks>
    /// JUNIOR: Este é o passo 1 do fluxo OAuth!
    ///
    /// Quando o usuário chama /api/auth/login/google:
    /// 1. ConfigureExternalAuthenticationProperties cria a URL de redirecionamento
    /// 2. Challenge() causa um redirecionamento HTTP 302 para o Google
    /// 3. O navegador vai para o Google e mostra a tela de login
    ///
    /// Parâmetros enviados ao provider:
    /// - client_id: identifica sua aplicação
    /// - redirect_uri: onde o Google vai redirecionar após login
    /// - response_type=code: pede um código (Authorization Code Flow)
    /// - scope: quais permissões você quer (email, profile, etc)
    /// - state: token anti-CSRF para segurança
    /// </remarks>
    /// <example>
    /// GET /api/auth/login/google?returnUrl=/dashboard
    /// Resultado: Redirecionamento para https://accounts.google.com/...
    /// </example>
    [HttpGet("login/{provider}")]
    public IActionResult Login(string provider, [FromQuery] string returnUrl = "/")
    {
        // Configura as propriedades de autenticação externa
        // Isso inclui: redirect URI, state token, scopes solicitados
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, returnUrl);

        // Challenge() inicia o fluxo de autenticação OAuth
        // É como "desafiar" o usuário a se autenticar com o provider
        return Challenge(properties, provider);
    }

    /// <summary>
    /// Callback do provider OAuth após autenticação bem-sucedida.
    /// O provider redireciona aqui com o código de autorização.
    /// </summary>
    /// <param name="provider">Nome do provider que completou a autenticação.</param>
    /// <returns>Dados do usuário autenticado ou erro.</returns>
    /// <remarks>
    /// JUNIOR: Este é o passo 6-10 do fluxo OAuth!
    ///
    /// Depois que o usuário faz login no Google:
    /// 1. Google redireciona para /api/auth/callback/google?code=XXX
    /// 2. GetExternalLoginInfoAsync() obtém as informações do usuário
    /// 3. Tentamos fazer login no Identity com essas informações
    ///
    /// FLUXO COMPLETO DESTE MÉTODO:
    ///
    /// Se usuário JÁ existe no banco:
    ///   - Adiciona o login externo a ele (AddLoginAsync)
    ///   - Faz login normal (ExternalLoginSignInAsync)
    ///
    /// Se usuário NÃO existe:
    ///   - Cria novo usuário com email do provider
    ///   - Adiciona o login externo ao novo usuário
    ///   - Adiciona à role "User"
    ///   - Faz login
    ///
    /// Este é o "Account Linking" básico - vincular contas externas
    /// a usuários locais.
    /// </remarks>
    /// <example>
    /// GET /api/auth/callback/google
    /// (Google chama esta URL com ?code=... após login)
    /// </example>
    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider)
    {
        // Obtém informações do usuário retornadas pelo provider
        // Contém: ProviderKey (identificador único no Google),
        // Principal (claims com email, nome, etc), ProviderName
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            // Falha ao obter informações - pode ser ataque CSRF ou erro de configuração
            return Unauthorized(new { message = "Falha ao obter informações externas" });
        }

        // Tentar fazer login com o provider externo
        // ProviderKey é o identificador único do usuário no Google
        // bypassTwoFactor: true = ignora 2FA se houver (para simplificação)
        var result = await _signInManager.ExternalLoginSignInAsync(
            provider.ToLowerInvariant(),  // Nome do provider em minúsculas
            info.ProviderKey,              // ID do usuário no provider
            isPersistent: false,           // Não lembrar login (sessão temporária)
            bypassTwoFactor: true);        // Pular verificação de dois fatores

        if (result.Succeeded)
        {
            // Login funcionou! Usuário já estava vinculado
            return Ok(new { message = $"Login via {provider} bem-sucedido", provider });
        }

        // Se não conseguiu fazer login, verificar se usuário existe pelo email
        // Claims são pedaços de informação sobre o usuário
        // O email pode vir em diferentes claims dependendo do provider
        var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value
            ?? info.Principal.FindFirst("email")?.Value
            ?? info.Principal.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrEmpty(email))
        {
            // Buscar usuário existente pelo email
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                // Usuário existe! Vincular login externo a esta conta
                // Isso permite que o mesmo usuário faça login com Google OU email/senha
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.ExternalLoginSignInAsync(
                    provider.ToLowerInvariant(),
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: true);
                return Ok(new { message = $"Login via {provider} bem-sucedido (usuário existente)", provider });
            }

            // Usuário não existe - criar novo!
            // typical flow: user signs in with OAuth for the first time
            var newUser = new User
            {
                Email = email,
                UserName = email.Split('@')[0],  // Usa parte antes do @ como username
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.Split('@')[0].ToUpperInvariant()
            };

            // Criar usuário no banco de dados
            var createResult = await _userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                // Vincular login externo ao novo usuário
                await _userManager.AddLoginAsync(newUser, info);
                // Adicionar à role padrão "User"
                await _userManager.AddToRoleAsync(newUser, "User");
                // Fazer login do usuário
                await _signInManager.ExternalLoginSignInAsync(
                    provider.ToLowerInvariant(),
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: true);
                return Ok(new { message = $"Usuário registrado via {provider}", provider });
            }
        }

        // Algo deu errado -，可能是 usuário bloqueado ou não permitido
        return BadRequest(new {
            message = "Falha ao processar autenticação externa",
            isLockedOut = result.IsLockedOut,
            isNotAllowed = result.IsNotAllowed
        });
    }

    /// <summary>
    /// Realiza logout do usuário atual.
    /// </summary>
    /// <returns>Mensagem de sucesso.</returns>
    /// <remarks>
    /// JUNIOR: Logout no OAuth
    /// O SignOutAsync() limpa apenas a sessão da SUA aplicação.
    /// NÃO faz logout no Google/Provider. O usuário continua logado lá.
    ///
    /// Para logout completo (incluindo provider), seria necessário:
    /// 1. Chamar o endpoint de logout do provider (se suportado)
    /// 2. Redirecionar para lá e depois voltar
    ///
    /// Mas geralmente limpar a sessão local é suficiente.
    /// </remarks>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logout realizado com sucesso" });
    }

    /// <summary>
    /// Retorna informações do usuário autenticado atual.
    /// </summary>
    /// <returns>Dados do usuário: email, nome, provider, status de autenticação.</returns>
    /// <remarks>
    /// JUNIOR: Claims - os "dados" do usuário
    /// Após login, o User.Identity está preenchido com Claims.
    /// Claims são declarações sobre o usuário: "este usuário tem email X",
    /// "tem nome Y", "autenticou via Google".
    ///
    /// ClaimTypes comuns:
    /// - ClaimTypes.Email: email do usuário
    /// - ClaimTypes.Name: nome de exibição
    /// - ClaimTypes.AuthenticationMethod: como autenticou ("Google", "Facebook")
    ///
    /// Você pode acessar claims assim:
    /// - User.FindFirst(ClaimTypes.Email)?.Value
    /// - User.Claims.Select(c => c.Type + "=" + c.Value)
    /// </remarks>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // User é uma propriedade do Controller que contém o principal atual
        // IsAuthenticated será true se há um usuário logado
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new { message = "Usuário não autenticado" });
        }

        // Extrair informações das claims do usuário
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        var provider = User.FindFirst(ClaimTypes.AuthenticationMethod)?.Value;

        return Ok(new
        {
            email,
            name,
            provider,
            isAuthenticated = true
        });
    }
}
