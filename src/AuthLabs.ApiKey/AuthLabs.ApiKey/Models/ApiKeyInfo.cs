namespace AuthLabs.ApiKey.Models;

/// <summary>
/// DTO (Data Transfer Object) com informacoes da API Key para retorno ao cliente.
/// </summary>
/// <remarks>
/// JUNIOR: O que e um DTO e por que usar?
/// - DTO e um objeto simples para TRANSFERIR dados entre camadas
/// - Este DTO e diferente da entidade ApiKey (que tem campos internos como Key, CreatedAt)
/// - Um DTO expõe SOMENTE os dados que o cliente precisa ver
/// - Tambem serve como "firewall" - o cliente nunca ve a estrutura completa do banco
/// - Padrao comum em APIs RESTful para controle do que e exposto
/// </remarks>
public class ApiKeyInfo
{
    /// <summary>
    /// ID da API Key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome do cliente/dono da API Key.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de escopos/permissões da API Key.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Por que usar List&lt;string&gt; aqui e não List&lt;ApiKeyScope&gt;?
    /// - Este DTO e para RETORNO ao cliente - nao precisa da entidade completa
    /// - O cliente so precisa saber os nomes dos scopes ("read", "write")
    /// - Menos dados transferidos = melhor performance
    /// - Nao exponha dados internos do banco sem necessidade
    /// </remarks>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// Role (funcao) da API Key.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Role e usada para:
    /// - [Authorize(Roles = "Admin")] em controllers
    /// - Decisoes de negocio baseadas em quem esta acessando
    /// - Auditoria e logs (saber quem fez o que)
    /// </remarks>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a API Key esta ativa.
    /// </summary>
    /// <remarks>
    /// JUNIOR: O cliente pode precisar saber se sua key esta ativa?
    /// - Sim! Para debugging quando a API retorna 401
    /// - Para mostrar status no painel do cliente
    /// - Para通知 sobre problemas de autenticacao
    /// </remarks>
    public bool IsActive { get; set; }
}
