namespace AuthLabs.Resource.Models;

/// <summary>
/// Representa um documento no sistema de gerenciamento de documentos.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Document"/> é o recurso principal deste sistema de autorização baseada em recursos.
/// Cada documento pertence a um usuário (OwnerId) e pode ter permissões específicas
/// concedidas a outros usuários através de <see cref="DocumentPermission"/>.
/// </para>
/// <para>
/// <b>Diferença entre RBAC e Autorização Baseada em Recursos:</b>
/// </para>
/// <list type="bullet">
/// <item>
/// <b>RBAC (Role-Based Access Control):</b> Permissões são atribuídas baseadas em funções/roles.
/// Exemplo: "Usuários com role 'Admin' podem excluir documentos".
/// A permissão é genérica e não depende do documento específico.
/// </item>
/// <item>
/// <b>Autorização Baseada em Recursos:</b> Permissões são evaluadas para um recurso específico.
/// Exemplo: "Este usuário pode excluir ESTE documento específico?".
/// A decisão depende tanto do usuário quanto do documento em questão.
/// </item>
/// </list>
/// <para>
/// Neste código, usamos autorização baseada em recursos porque precisamos verificar
/// se o usuário tem permissão para editar ou excluir um documento específico,
/// não apenas documentos em geral.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var doc = new Document
/// {
///     Title = "Relatório Q1",
///     Content = "Conteúdo do relatório...",
///     OwnerId = "user-123",
///     Permissions = new List&lt;DocumentPermission&gt;()
/// };
/// </code>
/// </example>
public class Document
{
    /// <summary>
    /// Identificador único do documento.
    /// </summary>
    /// <remarks>
    /// Este ID é gerado pelo banco de dados (auto-incremento) e serve como chave primária.
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// Título do documento.
    /// </summary>
    /// <remarks>
    /// O título é uma propriedade obrigatória e não pode ser vazio.
    /// </remarks>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Conteúdo textural do documento.
    /// </summary>
    /// <remarks>
    /// Pode conter texto formatado, código, ou qualquer conteúdo textual.
    /// </remarks>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Identificador do proprietário (dono) do documento.
    /// </summary>
    /// <remarks>
    /// <para>
    /// O OwnerId representa o ID do usuário que criou o documento.
    /// O proprietário TEM SEMPRE todas as permissões sobre seu próprio documento:
    /// pode editar, excluir e conceder permissões a outros.
    /// </para>
    /// <para>
    /// JUNIOR: Este é o campo-chave para autorização! O handler de autorização
    /// verifica primeiro se o userId do contexto é igual ao OwnerId.
    /// Se for, a autorização sempre succeeds (sucesso).
    /// </para>
    /// </remarks>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Data e hora de criação do documento em UTC.
    /// </summary>
    /// <remarks>
    /// Inicializado automaticamente com <see cref="DateTime.UtcNow"/> no momento da criação.
    /// </remarks>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Lista de permissões específicas concedidas a outros usuários.
    /// </summary>
    /// <remarks>
    /// <para>
    /// JUNIOR: Esta é a "magia" da autorização baseada em recursos!
    /// Cada <see cref="DocumentPermission"/> representa uma permissão específica
    /// (pode editar, pode excluir) para um usuário específico neste documento específico.
    /// </para>
    /// <para>
    /// Diferente de RBAC onde todos com role "Editor" podem editar TODOS os documentos,
    /// aqui apenas usuários específicos podem editar este documento específico.
    /// </para>
    /// </remarks>
    public List<DocumentPermission> Permissions { get; set; } = new();
}
