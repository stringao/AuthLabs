using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthLabs.Resource.Authorization;
using AuthLabs.Resource.Models;
using AuthLabs.Resource.Services;

namespace AuthLabs.Resource.Controllers;

/// <summary>
/// Controller da API REST para gerenciamento de documentos.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DocumentsController"/> expõe endpoints REST para operações com documentos.
/// Todos os endpoints requerem autenticação (atributo <c>[Authorize]</c>).
/// </para>
/// <para>
/// <b>JUNIOR: Arquitetura RESTful</b>
/// </para>
/// <list type="bullet">
/// <item><c>GET /api/documents</c> - Lista todos os documentos</item>
/// <item><c>GET /api/documents/{id}</c> - Obtém um documento específico</item>
/// <item><c>PUT /api/documents/{id}</c> - Atualiza um documento</item>
/// <item><c>DELETE /api/documents/{id}</c> - Exclui um documento</item>
/// <item><c>GET /api/documents/{id}/can-edit</c> - Verifica permissão de edição</item>
/// <item><c>GET /api/documents/{id}/can-delete</c> - Verifica permissão de exclusão</item>
/// </list>
/// <para>
/// <b>Sobre Autorização Baseada em Recursos neste Controller:</b>
/// </para>
/// <para>
/// Os métodos PUT e DELETE demonstram autorização baseada em recursos.
/// O endpoint recebe o documento específico e verifica se o usuário
/// logado tem permissão para realizar a operação NESSE documento específico.
/// </para>
/// <para>
/// <b>JUNIOR: Por que Authorization como serviço (IAuthorizationService)
/// em vez de atributo [Authorize]?</b>
/// </para>
/// <list type="bullet">
/// <item>
/// <c>[Authorize]</c> com políticas é para autorização ESTÁTICA -
/// você define "esta ação requer role Admin".
/// </item>
/// <item>
/// <c>IAuthorizationService.AuthorizeAsync</c> é para autorização DINÂMICA -
/// "este usuário pode fazer isto neste objeto específico?".
/// </item>
/// <item>
/// Como cada documento tem suas próprias permissões, precisamos
/// passar o documento específico para a verificação.
/// </item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    /// <summary>
    /// Serviço para operações com documentos.
    /// </summary>
    private readonly IDocumentService _documentService;

    /// <summary>
    /// Serviço de autorização do ASP.NET Core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: Para que serve IAuthorizationService?</b>
    /// É o serviço que permite verificar autorização passando o recurso específico.
    /// Diferente do atributo [Authorize] que é estático.
    /// </para>
    /// <para>
    ///调用方式:
    /// <code>await _authorizationService.AuthorizeAsync(User, document, requirement)</code>
    /// </para>
    /// </remarks>
    private readonly IAuthorizationService _authorizationService;

    /// <summary>
    /// Inicializa uma nova instância do controller de documentos.
    /// </summary>
    /// <param name="documentService">Serviço de documentos injetado.</param>
    /// <param name="authorizationService">Serviço de autorização injetado.</param>
    /// <remarks>
    /// JUNIOR: Injeção de dependência - o ASP.NET Core injeta automaticamente
    /// os serviços configurados no Program.cs.
    /// </remarks>
    public DocumentsController(IDocumentService documentService, IAuthorizationService authorizationService)
    {
        _documentService = documentService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Lista todos os documentos do sistema.
    /// </summary>
    /// <returns>Uma lista de todos os documentos.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: GET /api/documents</b>
    /// </para>
    /// <para>
    /// Este endpoint lista TODOS os documentos. Em um sistema real,
    /// você provavelmente filtraria para mostrar apenas documentos
    /// onde o usuário é proprietário ou tem alguma permissão.
    /// </para>
    /// <para>
    /// Note que não há verificação de autorização aqui - apenas
    /// verificação de autenticação ([Authorize]). Qualquer usuário
    /// autenticado pode ver todos os documentos.
    /// </para>
    /// </remarks>
    /// <response code="200">Retorna a lista de documentos.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var documents = await _documentService.GetAllDocumentsAsync();
        return Ok(documents);
    }

    /// <summary>
    /// Obtém um documento específico pelo ID.
    /// </summary>
    /// <param name="id">O ID do documento.</param>
    /// <returns>O documento se encontrado.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: GET /api/documents/{id}</b>
    /// </para>
    /// <para>
    /// Retorna 404 Not Found se o documento não existe.
    /// Sem verificação de propriedade/permissão - qualquer
    /// usuário autenticado pode ver qualquer documento.
    /// </para>
    /// </remarks>
    /// <response code="200">Retorna o documento.</response>
    /// <response code="404">Documento não encontrado.</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(new { message = "Documento não encontrado" });

        return Ok(document);
    }

    /// <summary>
    /// Atualiza um documento existente.
    /// </summary>
    /// <param name="id">O ID do documento a ser atualizado.</param>
    /// <param name="document">Os novos dados do documento.</param>
    /// <returns>O documento atualizado.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: PUT /api/documents/{id}</b>
    /// </para>
    /// <para>
    /// Este endpoint DEMONSTRA autorização baseada em recursos!
    /// Antes de atualizar, verificamos se o usuário tem permissão "Edit"
    /// para ESTE documento específico.
    /// </para>
    /// <para>
    /// <b>Fluxo de autorização:</b>
    /// </para>
    /// <list type="number">
    /// <item>Busca o documento do banco (precisamos dele para o handler)</item>
    /// <item>Extrai o userId do token JWT (User.FindFirst)</item>
    /// <item>Chama <c>_authorizationService.AuthorizeAsync</c> passando:
    /// <list type="bullet">
    /// <item>User - o principal autenticado</item>
    /// <item>existing - o documento específico</item>
    /// <item>new DocumentOperationRequirement("Edit") - a operação desejada</item>
    /// </list>
    /// </item>
    /// <item>Se <c>authResult.Succeeded</c> é false, retorna Forbid()</item>
    /// <item>Se true, procede com a atualização</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <response code="200">Documento atualizado com sucesso.</response>
    /// <response code="404">Documento não encontrado.</response>
    /// <response code="403">Usuário não tem permissão para editar este documento.</response>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Document document)
    {
        var existing = await _documentService.GetDocumentByIdAsync(id);
        if (existing == null)
            return NotFound(new { message = "Documento não encontrado" });

        // JUNIOR: Extrai o ID do usuário do token JWT
        // ClaimTypes.NameIdentifier é a claim que contém o ID único
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

        // JUNIOR: AQUI ACONTECE A AUTORIZAÇÃO BASEADA EM RECURSOS!
        // Passamos o documento específico para ser avaliado pelo handler
        var authResult = await _authorizationService.AuthorizeAsync(
            User, existing,
            new DocumentOperationRequirement("Edit"));

        if (!authResult.Succeeded)
            return Forbid();

        var updated = await _documentService.UpdateDocumentAsync(id, document);
        return Ok(updated);
    }

    /// <summary>
    /// Exclui um documento do sistema.
    /// </summary>
    /// <param name="id">O ID do documento a ser excluído.</param>
    /// <returns>Sem conteúdo em caso de sucesso.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: DELETE /api/documents/{id}</b>
    /// </para>
    /// <para>
    /// Similar ao Update, este endpoint usa autorização baseada em recursos.
    /// Verificamos se o usuário tem permissão "Delete" para o documento.
    /// </para>
    /// <para>
    /// <b>JUNIOR: Por que 204 No Content?</b>
    /// Em APIs REST, DELETE bem-sucedido tipicamente retorna 204 (No Content)
    /// porque não há nada mais a retornar - o recurso foi removido.
    /// </para>
    /// </remarks>
    /// <response code="204">Documento excluído com sucesso.</response>
    /// <response code="404">Documento não encontrado.</response>
    /// <response code="403">Usuário não tem permissão para excluir este documento.</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(new { message = "Documento não encontrado" });

        // JUNIOR: AUTORIZAÇÃO BASEADA EM RECURSOS!
        // Mesmo padrão do Update, mas com operação "Delete"
        var authResult = await _authorizationService.AuthorizeAsync(
            User, document,
            new DocumentOperationRequirement("Delete"));

        if (!authResult.Succeeded)
            return Forbid();

        await _documentService.DeleteDocumentAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Verifica se o usuário atual pode editar um documento.
    /// </summary>
    /// <param name="id">O ID do documento.</param>
    /// <returns>Um objeto indicando se o usuário pode editar.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: GET /api/documents/{id}/can-edit</b>
    /// </para>
    /// <para>
    /// Endpoint utilitário para o frontend verificar permissões
    /// antes de tentar uma operação. Retorna apenas um boolean.
    /// </para>
    /// <para>
    /// <b>JUNIOR: Por que precisamos verificar via service se já temos o handler?</b>
    /// O handler é para proteger operações (como Update). O service
    /// CanUserEditAsync é para perguntar "eu posso?" sem tentar a ação.
    /// Útil para mostrar/ocultar botões na UI.
    /// </para>
    /// </remarks>
    /// <response code="200">Retorna objeto com propriedade "canEdit".</response>
    [HttpGet("{id}/can-edit")]
    public async Task<IActionResult> CanEdit(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var canEdit = await _documentService.CanUserEditAsync(id, userId);
        return Ok(new { canEdit });
    }

    /// <summary>
    /// Verifica se o usuário atual pode excluir um documento.
    /// </summary>
    /// <param name="id">O ID do documento.</param>
    /// <returns>Um objeto indicando se o usuário pode excluir.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: GET /api/documents/{id}/can-delete</b>
    /// </para>
    /// <para>
    /// Mesma ideia do endpoint can-edit, mas para exclusão.
    /// </para>
    /// </remarks>
    /// <response code="200">Retorna objeto com propriedade "canDelete".</response>
    [HttpGet("{id}/can-delete")]
    public async Task<IActionResult> CanDelete(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var canDelete = await _documentService.CanUserDeleteAsync(id, userId);
        return Ok(new { canDelete });
    }
}
