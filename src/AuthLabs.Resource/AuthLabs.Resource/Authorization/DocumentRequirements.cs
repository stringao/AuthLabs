using Microsoft.AspNetCore.Authorization;

namespace AuthLabs.Resource.Authorization;

/// <summary>
/// Requisito de autorização para operações em documentos.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DocumentOperationRequirement"/> implementa <see cref="IAuthorizationRequirement"/>
/// e representa uma operação específica que um usuário deseja realizar em um documento.
/// </para>
/// <para>
/// <b>O que é IAuthorizationRequirement?</b>
/// É parte do sistema de autorização do ASP.NET Core. Um requisito é uma
/// condição que deve ser satisfeita para que a autorização seja concedida.
/// O framework não sabe como avaliar requisitos - isso é feito pelo
/// <see cref="DocumentAuthorizationHandler"/>.
/// </para>
/// <para>
/// <b>JUNIOR: Por que separar Requirement do Handler?</b>
/// </para>
/// <list type="bullet">
/// <item>
/// <b>Separação de responsabilidades:</b> O requisito define O QUE verificar,
/// o handler define COMO verificar.
/// </item>
/// <item>
/// <b>Reusabilidade:</b> O mesmo requisito pode ter diferentes handlers
/// em diferentes contextos.
/// </item>
/// <item>
/// <b>Testabilidade:</b> Você pode testar requisitos independentemente dos handlers.
/// </item>
/// </list>
/// <para>
/// <b>JUNIOR: Comparação com RBAC</b>
/// Em um sistema RBAC tradicional, você teria requisitos como:
/// <c>RequireRole("Admin")</c> ou <c>RequireClaim("permission", "delete-document")</c>.
/// Estes são genéricos e não se referem a um recurso específico.
/// </para>
/// <para>
/// Com autorização baseada em recursos, nosso requisito inclui a operação
/// (Edit, Delete) e o handlerrecebe o documento específico para verificar
/// se o usuário PODE executar essa operação NESSE documento.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Exemplo de uso com AuthorizeAttribute
/// [Authorize(Policy = "DocumentEdit")]
/// public IActionResult Edit(int id) { ... }
///
/// // Ou via serviço de autorização
/// await authorizationService.AuthorizeAsync(user, document,
///     new DocumentOperationRequirement("Edit"));
/// </code>
/// </example>
public class DocumentOperationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// A operação que está sendo tentada no documento.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Valores típicos: "Edit", "Delete", "View", "Share"
    /// </para>
    /// <para>
    /// JUNIOR: Esta string é comparada diretamente no handler.
    /// Se você adicionar novas operações (ex: "Share"), precisará
    /// modificar o <see cref="DocumentAuthorizationHandler"/> para tratá-las.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Operações definidas no sistema
    /// var editReq = new DocumentOperationRequirement("Edit");
    /// var deleteReq = new DocumentOperationRequirement("Delete");
    /// </code>
    /// </example>
    public string Operation { get; }

    /// <summary>
    /// Cria um novo requisito de operação de documento.
    /// </summary>
    /// <param name="operation">A operação que está sendo requerida (ex: "Edit", "Delete").</param>
    /// <remarks>
    /// JUNIOR: O construtor é simples - apenas armazena a operação.
    /// A validação real acontece no handler, que recebe o documento
    /// e verifica se o usuário tem permissão para essa operação.
    /// </remarks>
    public DocumentOperationRequirement(string operation)
    {
        Operation = operation;
    }
}
