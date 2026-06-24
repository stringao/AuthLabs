using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Windows.Controllers;

/// <summary>
/// Controller com endpoints protegidos por autenticacao Windows.
/// Demonstra diferentes nivel de autorizacao baseados em roles.
/// </summary>
/// <remarks>
/// JUNIOR: Este controller mostra como proteger endpoints de diferentes formas:
///
/// 1. Protecao no nivel do Controller (classe):
///    [Authorize(AuthenticationSchemes = "Negotiate")]
///    = Todos os endpoints requerem Windows Authentication
///
/// 2. Protecao no nivel do Endpoint (metodo):
///    [Authorize(Roles = "Admin")]
///    = Apenas usuarios com role "Admin" podem acessar
///
/// 3. Permitir acesso anonimo:
///    [AllowAnonymous]
///    = Endpoint pode ser acessado sem autenticacao
///
/// HIERARQUIA DE ATRIBUTOS:
/// [AllowAnonymous] &gt; [Authorize] (inherited nao e aplicado se AllowAnonymous presente)
///
/// Se AllowAnonymous esta em QUALQUER lugar do controller ou metodo,
/// aquele endpoint especifico ignora toda a authorize do controller.
/// </remarks>
[ApiController]
[Authorize(AuthenticationSchemes = "Negotiate")]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Endpoint protegido que requer autenticacao Windows.
    /// Qualquer usuario Windows autenticado pode acessar.
    /// </summary>
    /// <returns>
    /// JSON com informacoes basicas do usuario autenticado.
    /// </returns>
    /// <remarks>
    /// JUNIOR: Este endpoint usa heranca de autorizacao do controller.
    /// O atributo [Authorize] no nivel da classe se aplica a todos os metodos,
    /// a menos que um metodo especifica [AllowAnonymous].
    ///
    /// User e uma propriedade do ControllerBase que contem o ClaimsPrincipal
    /// do usuario atual. Ele e populado automaticamente pelo middleware
    /// de autenticacao.
    ///
    /// User.Identity contem informacoes basicas:
    /// - Name: Nome do usuario (DOMINIO\Usuario)
    /// - AuthenticationType: "Negotiate" (Kerberos/NTLM)
    /// - IsAuthenticated: true/false
    /// </remarks>
    [HttpGet]
    public IActionResult Get()
    {
        var user = User.Identity?.Name;
        var authType = User.Identity?.AuthenticationType;
        return Ok(new
        {
            user,
            authType,
            message = "Autenticado via Windows"
        });
    }

    /// <summary>
    /// Area administrativa - apenas para usuarios com role Admin.
    /// </summary>
    /// <returns>
    /// JSON com mensagem de sucesso e nome do usuario admin.
    /// </returns>
    /// <response code="200">Retorna sucesso se usuario tem role Admin.</response>
    /// <response code="403">Retorna 403 se usuario autenticado mas sem role Admin.</response>
    /// <remarks>
    /// JUNIOR: [Authorize(Roles = "Admin")] e uma Authorization Policy.
    ///
    /// DIFERENCA ENTRE:
    /// - [Authorize] = apenas precisa estar autenticado
    /// - [Authorize(Roles = "Admin")] = precisa estar autenticado E ter role Admin
    ///
    /// COMO VERIFICAR ROLES:
    /// O ASP.NET Core verifica claims ClaimTypes.Role com valor "Admin".
    /// O WindowsClaimsTransformer adiciona esta claim baseando-se
    /// no nome do usuario (contem "Admin" = Admin role).
    ///
    /// FLUXO DE AUTORIZACAO:
    /// 1. Request chega ao endpoint
    /// 2. Authentication Middleware valida Windows credentials
    /// 3. ClaimsTransformer adiciona claims de Role
    /// 4. Authorization Middleware verifica: usuario tem claim Role="Admin"?
    /// 5. Se sim: executa o metodo
    /// 6. Se nao: retorna 403 Forbidden
    ///
    /// NOTA: 403 significa "autenticado mas nao autorizado".
    /// 401 significa "nao autenticado".
    /// </remarks>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdmin()
    {
        return Ok(new
        {
            message = "Area administrativa",
            user = User.Identity?.Name
        });
    }

    /// <summary>
    /// Area publica - qualquer usuario pode acessar, inclusive anonimos.
    /// </summary>
    /// <returns>
    /// JSON com mensagem indicando area publica.
    /// </returns>
    /// <remarks>
    /// JUNIOR: [AllowAnonymous] sobrescreve a autorizacao do controller.
    ///
    /// MESMO que o controller tenha [Authorize], o metodo com
    /// [AllowAnonymous] pode ser acessado por qualquer pessoa.
    ///
    /// CASOS DE USO COMUNS:
    /// - Paginas de disclaimer/termos de uso
    /// - Endpoints de health check
    /// - Documentacao da API
    /// - Login page (mostrar opcoes de autenticacao)
    ///
    /// CUIDADO: Nao coloque informacoes sensiveis em endpoints AllowAnonymous!
    /// </remarks>
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult GetPublic()
    {
        return Ok(new
        {
            message = "Area publica - sem autenticacao"
        });
    }
}
