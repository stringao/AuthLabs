using Microsoft.EntityFrameworkCore;
using AuthLabs.Resource.Data;
using AuthLabs.Resource.Models;

namespace AuthLabs.Resource.Services;

public class DocumentService : IDocumentService
{
    private readonly ResourceDbContext _context;

    public DocumentService(ResourceDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
    {
        return await _context.Documents
            .Include(d => d.Permissions)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(int id)
    {
        return await _context.Documents
            .Include(d => d.Permissions)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Document> CreateDocumentAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<Document?> UpdateDocumentAsync(int id, Document document)
    {
        var existing = await _context.Documents
            .Include(d => d.Permissions)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (existing == null) return null;

        existing.Title = document.Title;
        existing.Content = document.Content;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null) return false;

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CanUserEditAsync(int documentId, string userId)
    {
        var document = await GetDocumentByIdAsync(documentId);
        if (document == null) return false;

        if (document.OwnerId == userId) return true;

        var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
        return permission?.CanEdit == true;
    }

    public async Task<bool> CanUserDeleteAsync(int documentId, string userId)
    {
        var document = await GetDocumentByIdAsync(documentId);
        if (document == null) return false;

        if (document.OwnerId == userId) return true;

        var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
        return permission?.CanDelete == true;
    }
}
