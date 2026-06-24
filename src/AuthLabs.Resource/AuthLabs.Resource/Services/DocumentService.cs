using Microsoft.EntityFrameworkCore;
using AuthLabs.Resource.Data;
using AuthLabs.Resource.Models;

namespace AuthLabs.Resource.Services;

/// <summary>
/// Serviço para gerenciamento de documentos.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DocumentService"/> implementa <see cref="IDocumentService"/> e contém
/// a lógica de negócio para operações CRUD (Create, Read, Update, Delete) em documentos.
/// </para>
/// <para>
/// <b>JUNIOR: Por que usar um serviço em vez de acessar o DbContext diretamente?</b>
/// </para>
/// <list type="bullet">
/// <item>
/// <b>Encapsulamento:</b> O controller não precisa saber como os dados são persistidos.
/// </item>
/// <item>
/// <b>Testabilidade:</b> Você pode criar um mock de IDocumentService para testes.
/// </item>
/// <item>
/// <b>Reutilização:</b> A mesma lógica pode ser usada em diferentes controllers/endpoints.
/// </item>
/// <item>
/// <b>Separação de responsabilidades:</b> Controllers lidam com HTTP, Services com dados.
/// </item>
/// </list>
/// <para>
/// <b>JUNIOR: Entity Framework Core no serviço</b>
/// O serviço usa <see cref="ResourceDbContext"/> para se comunicar com o banco de dados.
/// O método <c>.Include(d => d.Permissions)</c> é importante - ele carrega
/// as permissões do documento junto com o documento (eager loading),
/// evitando erros quando o handler de autorizaçãoaccessa <c>document.Permissions</c>.
/// </para>
/// </remarks>
public class DocumentService : IDocumentService
{
    /// <summary>
    /// Contexto do banco de dados do Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// JUNIOR: DbContext é como uma "sessão" com o banco de dados.
    /// Ele rastreia mudanças e pode persisti-las com SaveChangesAsync().
    /// </remarks>
    private readonly ResourceDbContext _context;

    /// <summary>
    /// Inicializa uma nova instância do serviço de documentos.
    /// </summary>
    /// <param name="context">Contexto do banco de dados.</param>
    /// <remarks>
    /// JUNIOR: Este é "Injeção de Dependência" em ação!
    /// O ASP.NET Core automaticamente injeta o DbContext configurado.
    /// Você nunca cria DocumentService com "new" manualmente (exceto em testes).
    /// </remarks>
    public DocumentService(ResourceDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtém todos os documentos do sistema com suas permissões.
    /// </summary>
    /// <returns>Uma coleção de todos os documentos.</returns>
    /// <remarks>
    /// <para>
    /// JUNIOR: Note o uso de <c>.Include(d => d.Permissions)</c>.
    /// Isto é "Eager Loading" - carrega os dados relacionados imediatamente.
    /// Sem isso, ao acessar <c>document.Permissions</c> no handler,
    /// você teria um erro ou dados vazios (lazy loading precisa de nova query).
    /// </para>
    /// <para>
    /// <b>Cuidados:</b> Este método retorna TODOS os documentos.
    /// Em produção, considere adicionar paginação para evitar
    /// carregar milhares de registros de uma vez.
    /// </para>
    /// </remarks>
    public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
    {
        return await _context.Documents
            .Include(d => d.Permissions)  // JUNIOR: Carrega permissões junto!
            .ToListAsync();
    }

    /// <summary>
    /// Obtém um documento específico pelo ID, incluindo suas permissões.
    /// </summary>
    /// <param name="id">O ID do documento.</param>
    /// <returns>O documento se encontrado, ou <c>null</c> caso contrário.</returns>
    /// <remarks>
    /// <para>
    /// JUNIOR: FirstOrDefaultAsync retorna null se não encontrar nenhum documento
    /// com o ID especificado. Diferente de FirstAsync que lança exceção.
    /// </para>
    /// <para>
    /// <c>.Include(d => d.Permissions)</c> é ESSENCIAL aqui porque o
    /// <see cref="DocumentAuthorizationHandler"/> precisa acessar
    /// <c>document.Permissions</c> para verificar autorização.
    /// </para>
    /// </remarks>
    public async Task<Document?> GetDocumentByIdAsync(int id)
    {
        return await _context.Documents
            .Include(d => d.Permissions)  // JUNIOR: Essencial para o handler!
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <summary>
    /// Cria um novo documento.
    /// </summary>
    /// <param name="document">Os dados do documento a ser criado.</param>
    /// <returns>O documento criado com o ID atribuído.</returns>
    /// <remarks>
    /// <para>
    /// JUNIOR: O documento já deve vir com <c>OwnerId</c> configurado!
    /// O controller normalmente define isso baseado no usuário logado.
    /// </para>
    /// <para>
    /// O ID é gerado automaticamente pelo banco (auto-incremento).
    /// </para>
    /// </remarks>
    public async Task<Document> CreateDocumentAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    /// <summary>
    /// Atualiza um documento existente.
    /// </summary>
    /// <param name="id">O ID do documento a ser atualizado.</param>
    /// <param name="document">Os novos dados do documento.</param>
    /// <returns>O documento atualizado, ou <c>null</c> se não encontrado.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: Por que não usar document.Id diretamente?</b>
    /// Porque o parâmetro <c>id</c> vem da URL (rota), enquanto
    /// <c>document</c> vem do body da requisição. Eles devem ser iguais,
    /// mas usamos o <c>id</c> da rota para garantir segurança
    /// (evitar que alguém altere o ID no body).
    /// </para>
    /// <para>
    /// <b>Importante:</b> Este método NÃO atualiza permissões.
    /// Apenas Title e Content são modificados. Para gerenciar
    /// permissões, seria necessário endpoints adicionais.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Exclui um documento do sistema.
    /// </summary>
    /// <param name="id">O ID do documento a ser excluído.</param>
    /// <returns><c>true</c> se excluido com sucesso, <c>false</c> se não encontrado.</returns>
    /// <remarks>
    /// <para>
    /// JUNIOR: FindAsync é mais simples que FirstOrDefaultAsync porque
    /// busca apenas pela chave primária (Id). É otimizado para este caso.
    /// </para>
    /// <para>
    /// <b>CUIDADO:</b> Esta operação é permanente (DELETE do banco)!
    /// As permissões associadas são removidas em cascata se configurado
    /// no DbContext, ou precisam ser removidas manualmente antes.
    /// </para>
    /// </remarks>
    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null) return false;

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Verifica se um usuário pode editar um documento específico.
    /// </summary>
    /// <param name="documentId">O ID do documento.</param>
    /// <param name="userId">O ID do usuário.</param>
    /// <returns><c>true</c> se o usuário pode editar, <c>false</c> caso contrário.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: Por que este método existe?</b>
    /// Este método é usado para endpoints de "verificação" (can-edit).
    /// Permite ao frontend saber se deve mostrar o botão de editar,
    /// sem tentar fazer a operação e receber erro 403.
    /// </para>
    /// <para>
    /// <b>Lógica:</b>
    /// </para>
    /// <list type="number">
    /// <item>Se o documento não existe: false</item>
    /// <item>Se o usuário é o proprietário: true</item>
    /// <item>Se tem permissão CanEdit = true: true</item>
    /// <item>Caso contrário: false</item>
    /// </list>
    /// </remarks>
    public async Task<bool> CanUserEditAsync(int documentId, string userId)
    {
        var document = await GetDocumentByIdAsync(documentId);
        if (document == null) return false;

        // JUNIOR: Mesma lógica do handler de autorização!
        // Proprietário sempre pode editar
        if (document.OwnerId == userId) return true;

        var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
        return permission?.CanEdit == true;
    }

    /// <summary>
    /// Verifica se um usuário pode excluir um documento específico.
    /// </summary>
    /// <param name="documentId">O ID do documento.</param>
    /// <param name="userId">O ID do usuário.</param>
    /// <returns><c>true</c> se o usuário pode excluir, <c>false</c> caso contrário.</returns>
    /// <remarks>
    /// <para>
    /// <b>JUNIOR: Mesma estrutura de CanUserEditAsync!</b>
    /// A única diferença é que verifica <c>CanDelete</c> em vez de <c>CanEdit</c>.
    /// </para>
    /// <para>
    /// Note que editar e deletar são permissões SEPARADAS.
    /// Um usuário pode ter permissão de edição mas não de exclusão.
    /// </para>
    /// </remarks>
    public async Task<bool> CanUserDeleteAsync(int documentId, string userId)
    {
        var document = await GetDocumentByIdAsync(documentId);
        if (document == null) return false;

        // JUNIOR: Mesma lógica do handler de autorização!
        // Proprietário sempre pode excluir
        if (document.OwnerId == userId) return true;

        var permission = document.Permissions.FirstOrDefault(p => p.UserId == userId);
        return permission?.CanDelete == true;
    }
}
