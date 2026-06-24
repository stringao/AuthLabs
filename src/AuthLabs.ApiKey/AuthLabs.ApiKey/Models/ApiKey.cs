namespace AuthLabs.ApiKey.Models;

/// <summary>
/// Modelo de entidade que representa uma API Key no banco de dados.
/// </summary>
/// <remarks>
/// JUNIOR: Por que "Model" e nao apenas "class"?
/// - Models representam dados que serao armazenados/recuperados do banco
/// - Esta classe usa Entity Framework Core para mapeamento ORM
/// - O nome "ApiKey" corresponde ao nome da tabela no banco
/// </remarks>
public class ApiKey
{
    /// <summary>
    /// Identificador unico da API Key no banco de dados.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Por que usar int ao inves de Guid?
    /// - ints sao mais faceis de ler/depurar
    /// - Performance slightly melhor para indices
    /// - Porem Guid evita enumeracao de IDs em testes de seguranca
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// Hash SHA256 da API Key, **NUNCA** armazene a chave em texto plano.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Seguranca fundamental!
    /// - Hashing e ONE-WAY: nao da pra reverter para descobrir a key original
    /// - SHA256 e um algoritmo de hash criptografico forte
    /// - Se o banco for comprometido, as chaves reais NAO poderao ser recuperadas
    /// </remarks>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Nome descritivo do cliente/dono desta API Key.
    /// Usado para identificacao e logs.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a API Key esta ativa ou desativada.
    /// Apenas chaves ativas podem ser usadas para autenticacao.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Data e hora em que a API Key expira.
    /// Apos este momento, a chave NAO sera mais valida mesmo se IsActive=true.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Por que combinar IsActive E ExpiresAt?
    /// - IsActive permite desativar temporariamente sem perder a configuracao
    /// - ExpiresAt permite expiracao automatica (revogacao implícita)
    /// - Ambas condicoes devem ser verdadeiras para a chave ser valida
    /// </remarks>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Data e hora de criacao da API Key.
    /// Usado para auditoria e logs.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Lista de escopos/permissões associados a esta API Key.
    /// </summary>
    /// <remarks>
    /// JUNIOR: O que sao escopos (scopes)?
    /// - Escopos definem QUAIS acoes a API Key pode realizar
    /// - Exemplos: "read" (ler dados), "write" (criar/editar), "delete" (remover)
    /// - Uma chave pode ter multiplos escopos separados por virgula
    /// - Exemplo: uma chave de admin teria "read", "write", "delete"
    /// </remarks>
    public List<ApiKeyScope> Scopes { get; set; } = new();

    /// <summary>
    /// Role (função) associada à API Key para controle de acesso baseado em roles.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Roles vs Scopes - qual a diferenca?
    /// - SCOPES: controlam permissoes em nivel de OPERACAO (read, write, delete)
    /// - ROLES: controlam permissoes em nivel de USUARIO/CLIENTE (Admin, User, Guest)
    /// - Roles sao usadas com [Authorize(Roles = "Admin")] em controllers
    /// - Scopes sao verificados manualmente no codigo da aplicacao
    /// </remarks>
    public string Role { get; set; } = "User";
}
