using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthLabs.Resource.Authorization;
using AuthLabs.Resource.Models;
using AuthLabs.Resource.Services;

namespace AuthLabs.Resource.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IAuthorizationService _authorizationService;

    public DocumentsController(IDocumentService documentService, IAuthorizationService authorizationService)
    {
        _documentService = documentService;
        _authorizationService = authorizationService;
    }

    // GET /api/documents - Lista todos os documentos (autenticados)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var documents = await _documentService.GetAllDocumentsAsync();
        return Ok(documents);
    }

    // GET /api/documents/{id} - Detalhe do documento
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(new { message = "Documento não encontrado" });

        return Ok(document);
    }

    // PUT /api/documents/{id} - Edita documento (requer permissão)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Document document)
    {
        var existing = await _documentService.GetDocumentByIdAsync(id);
        if (existing == null)
            return NotFound(new { message = "Documento não encontrado" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

        // Usa Resource-based authorization
        var authResult = await _authorizationService.AuthorizeAsync(
            User, existing,
            new DocumentOperationRequirement("Edit"));

        if (!authResult.Succeeded)
            return Forbid();

        var updated = await _documentService.UpdateDocumentAsync(id, document);
        return Ok(updated);
    }

    // DELETE /api/documents/{id} - Remove documento (requer permissão)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(new { message = "Documento não encontrado" });

        // Usa Resource-based authorization
        var authResult = await _authorizationService.AuthorizeAsync(
            User, document,
            new DocumentOperationRequirement("Delete"));

        if (!authResult.Succeeded)
            return Forbid();

        await _documentService.DeleteDocumentAsync(id);
        return NoContent();
    }

    // GET /api/documents/{id}/can-edit - Verifica se pode editar
    [HttpGet("{id}/can-edit")]
    public async Task<IActionResult> CanEdit(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var canEdit = await _documentService.CanUserEditAsync(id, userId);
        return Ok(new { canEdit });
    }

    // GET /api/documents/{id}/can-delete - Verifica se pode deletar
    [HttpGet("{id}/can-delete")]
    public async Task<IActionResult> CanDelete(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var canDelete = await _documentService.CanUserDeleteAsync(id, userId);
        return Ok(new { canDelete });
    }
}
