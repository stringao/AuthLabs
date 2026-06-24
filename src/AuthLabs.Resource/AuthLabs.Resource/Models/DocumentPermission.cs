namespace AuthLabs.Resource.Models;

/// <summary>
/// Representa uma permissão específica de um usuário sobre um documento.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DocumentPermission"/> é usada para implementar autorização baseada em recursos.
/// Ela define QUAIS operações um usuário não-proprietário pode executar em um documento específico.
/// </para>
/// <para>
/// <b>Arquitetura de Permissões:</b>
/// </para>
/// <list type="number">
/// <item>Proprietário (Owner) - Tem todas as permissões automaticamente</item>
/// <item>Usuários com permissão explícita - Têm apenas as permissões definidas aqui</item>
/// <item>Usuários sem permissão - Não podem acessar o documento</item>
/// </list>
/// <para>
/// <b>JUNIOR: Por que não apenas usar roles?</b>
/// Imagine um sistema onde você quer que Maria possa editar o Documento A,
/// mas NÃO possa editar o Documento B. Com RBAC puro, se Maria tem role "Editor",
/// ela editarix TODOS os documentos. Com autorização baseada em recursos,
/// verificamos especificamente se Maria tem permissão no Documento B.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Exemplo: Permitir que usuário "user-456" edite o documento 10
/// var permission = new DocumentPermission
/// {
///     DocumentId = 10,
///     UserId = "user-456",
///     CanEdit = true,
///     CanDelete = false
/// };
/// </code>
/// </example>
public class DocumentPermission
{
    /// <summary>
    /// Identificador único da permissão.
    /// </summary>
    /// <remarks>
    /// Chave primária da tabela de permissões.
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// Identificador do documento ao qual esta permissão se refere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// JUNIOR: Este campo cria a relação entre permissão e documento.
    /// Uma permissão SEMPRE pertence a um documento específico.
    /// </para>
    /// <para>
    /// Este é o campo de junção (foreign key) que conecta
    /// <see cref="DocumentPermission"/> a <see cref="Document"/>.
    /// </para>
    /// </remarks>
    public int DocumentId { get; set; }

    /// <summary>
    /// Navegação para o documento associado.
    /// </summary>
    /// <remarks>
    /// Propriedade de navegação do Entity Framework Core.
    /// Permite acessar o documento completo através da permissão.
    /// </remarks>
    public Document Document { get; set; } = null!;

    /// <summary>
    /// Identificador do usuário que recebe esta permissão.
    /// </summary>
    /// <remarks>
    /// <para>
    /// JUNIOR: Este é o usuário que GANHA a permissão.
    /// O sistema verifica se o userId do contexto de autenticação
    /// corresponde a este campo para permitir ações.
    /// </para>
    /// <para>
    /// O usuário com este ID pode apenas as ações marcadas como true
    /// (<see cref="CanEdit"/> e <see cref="CanDelete"/>).
    /// </para>
    /// </remarks>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Indica se o usuário pode editar o documento.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Quando <c>true</c>, o usuário especificado em <see cref="UserId"/>
    /// pode modificar o título e conteúdo do documento.
    /// </para>
    /// <para>
    /// JUNIOR: Esta permissão NÃO inclui deletar! Um usuário pode ter
    /// CanEdit=true mas CanDelete=false, significando que pode editar
    /// mas não pode excluir o documento.
    /// </para>
    /// </remarks>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Indica se o usuário pode excluir o documento.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Quando <c>true</c>, o usuário especificado em <see cref="UserId"/>
    /// pode excluir permanentemente o documento do sistema.
    /// </para>
    /// <para>
    /// JUNIOR: Excluir é uma permissão separada de editar! Isto permite
    /// cenários como: "Este usuário pode editar o documento mas não excluí-lo".
    /// </para>
    /// </remarks>
    public bool CanDelete { get; set; }
}
