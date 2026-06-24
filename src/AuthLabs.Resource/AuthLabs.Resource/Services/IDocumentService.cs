using AuthLabs.Resource.Models;

namespace AuthLabs.Resource.Services;

public interface IDocumentService
{
    Task<IEnumerable<Document>> GetAllDocumentsAsync();
    Task<Document?> GetDocumentByIdAsync(int id);
    Task<Document> CreateDocumentAsync(Document document);
    Task<Document?> UpdateDocumentAsync(int id, Document document);
    Task<bool> DeleteDocumentAsync(int id);
    Task<bool> CanUserEditAsync(int documentId, string userId);
    Task<bool> CanUserDeleteAsync(int documentId, string userId);
}
