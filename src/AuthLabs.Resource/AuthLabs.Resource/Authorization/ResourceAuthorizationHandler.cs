using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using AuthLabs.Resource.Models;

namespace AuthLabs.Resource.Authorization;

/// <summary>
/// Manipulador de autorização que avalia permissões em documentos específicos.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DocumentAuthorizationHandler"/> é o coração da autorização baseada em recursos.
/// Ele implementa <see cref="AuthorizationHandler{DocumentOperationRequirement, Document}"/>,
/// onde Document é o tipo do recurso sendo protegido.
/// </para>
/// <para>
/// <b>Fluxo de Autorização Baseada em Recursos:</b>
/// </para>
/// <list type="number">
/// <item>Controller chama <c>authorizationService.AuthorizeAsync(User, document, requirement)</c></item>
/// <item>O framework identifica o handler apropriado (<see cref="DocumentAuthorizationHandler"/>)</item>
/// <item>O handlerrecebe o contexto, o requisito E o documento específico</item>
/// <item>O handler verifica: "Este usuário pode fazer esta operação neste documento?"</item>
/// <item>O handler chama <c>context.Succeed(requirement)</c> se autorizado</item>
/// </list>
/// <para>
/// <b>JUNIOR: Por que o documento é passado como parâmetro?</b>
/// </para>
/// <list type="bullet">
/// <item>
/// Em RBAC, você verifica apenas: "O usuário tem role X?" - não precisa saber QUAL recurso.
/// </item>
/// <item>
/// Em autorização baseada em recursos, você precisa saber: "O usuário tem permissão
/// para editar ESTE documento específico (não outro)?"
/// </item>
/// <item>
/// Por isso o handler recebe o <c>Document</c> - para examinar suas permissões específicas.
/// </item>
/// </list>
/// <para>
/// <b>JUNIOR: A ordem de verificação é importante!</b>
/// </para>
/// <list type="number">
/// <item>Primeiro: Verificar se é o proprietário (Owner) - se sim, sempre permite</item>
/// <item>Segundo: Verificar permissões explícitas na lista <c>document.Permissions</c>
/// para a operação específica requerida
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Exemplo de uso no Controller:
/// var authResult = await _authorizationService.AuthorizeAsync(
///     User,                          // ClaimsPrincipal do usuário
///     document,                      // O recurso (Document) sendo acessado
///     new DocumentOperationRequirement("Edit"));  // O que quer fazer
///
/// if (!authResult.Succeeded)
///     return Forbid();
/// </code>
/// </example>
public class DocumentAuthorizationHandler : AuthorizationHandler<DocumentOperationRequirement, Document>
{
    /// <summary>
    /// Acessor HTTP para acessar informações da requisição atual.
    /// </summary>
    /// <remarks>
    /// <para>
    /// JUNIOR: Para que serve IHttpContextAccessor?
    /// Em handlers de autorização, você normalmente não tem acesso direto ao HttpContext.
    /// Este serviço permite acessar informações como headers, cookies, etc.
    /// Neste handler específico, não é usado ativamente, mas está disponível
    /// para cenários onde você precise de informações adicionais da requisição.
    /// </para>
    /// </remarks>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância do manipulador de autorização.
    /// </summary>
    /// <param name="httpContextAccessor">Acessor para o contexto HTTP.</param>
    public DocumentAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Avalia se o usuário atende ao requisito para o documento especificado.
    /// </summary>
    /// <param name="context">Contexto da avaliação de autorização.</param>
    /// <param name="requirement">O requisito a ser avaliado (ex: "Edit", "Delete").</param>
    /// <param name="document">O documento específico sendo acessado.</param>
    /// <returns>Task representando a operação assíncrona.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: Este é o método principal do handler!</b>
    /// Aqui acontece toda a lógica de decisão de autorização.
    /// </para>
    /// <para>
    /// <b>Passo a passo da verificação:</b>
    /// </para>
    /// <list type="number">
    /// <item>
    /// <c>var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value</c>
    /// <br/>Extrai o ID do usuário logado das claims do token JWT.
    /// </item>
    /// <item>
    /// <c>if (document.OwnerId == userId)</c>
    /// <br/>Verifica se o usuário é o proprietário. Se SIM, concede todas as permissões.
    /// </item>
    /// <item>
    /// <c>var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId)</c>
    /// <br/>Busca uma permissão explícita para este usuário neste documento.
    /// </item>
    /// <item>
    /// <c>if (requirement.Operation == "Edit" &amp;&amp; permission?.CanEdit == true)</c>
    /// <br/>Se a operação é "Edit", verifica se CanEdit está true.
    /// </item>
    /// <item>
    /// <c>if (requirement.Operation == "Delete" &amp;&amp; permission?.CanDelete == true)</c>
    /// <br/>Se a operação é "Delete", verifica se CanDelete está true.
    /// </item>
    /// </list>
    /// <para>
    /// <b>Nota importante:</b> Se o usuário NÃO é o proprietário E NÃO tem permissão,
    /// o método NÃO chama <c>context.Succeed()</c>, resultando em falha na autorização.
    /// </para>
    /// </remarks>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentOperationRequirement requirement,
        Document document)
    {
        // JUNIOR: FindFirst procura uma claim específica no token JWT
        // ClaimTypes.NameIdentifier contém o ID único do usuário
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // JUNIOR: Proprietário tem TODAS as permissões automaticamente
        // Isto é uma regra de negócio: o dono do documento sempre pode fazer tudo
        if (document.OwnerId == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // JUNIOR: Busca permissão específica para este usuário no documento
        // LINQ: FirstOrDefault retorna null se não encontrar - por isso usamos ?.
        var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);

        // JUNIOR: Verifica a permissão baseado na operação requerida
        // Operações possíveis: "Edit" e "Delete" (case-sensitive!)
        if (requirement.Operation == "Edit" && permission?.CanEdit == true)
            context.Succeed(requirement);
        else if (requirement.Operation == "Delete" && permission?.CanDelete == true)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
