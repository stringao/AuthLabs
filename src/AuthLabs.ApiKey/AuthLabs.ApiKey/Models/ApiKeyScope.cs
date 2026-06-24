namespace AuthLabs.ApiKey.Models;

/// <summary>
/// Modelo que representa um ESCOPO (permissao) associado a uma API Key.
/// </summary>
/// <remarks>
/// JUNIOR: Por que criar uma tabela separada para escopos?
/// - Relacionamento 1:N: uma API Key pode ter VARIOS escopos
/// - Se fossemos guardar scopes como string ("read,write,delete"), seria mais dificil:
    /// - Buscar todas as chaves com scope "delete"
    /// - Adicionar/remover scopes individuais
    /// - Garantir consistencia dos dados
/// - Esta e uma relacao de composicao (ApiKey TEM Scopes)
/// </remarks>
public class ApiKeyScope
{
    /// <summary>
    /// Identificador unico do escopo no banco de dados.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID da API Key a qual este escopo pertence.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Foreign Key (chave estrangeira)
    /// - Este campo conecta ApiKeyScope a uma ApiKey especifica
    /// - No Entity Framework, isso cria um relacionamento entre tabelas
    /// - Cada escopo pertence a EXATAMENTE uma API Key
    /// </remarks>
    public int ApiKeyId { get; set; }

    /// <summary>
    /// Navegação para a API Key pai deste escopo.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Para que serve a property de navegacao?
    /// - Permite acessar a ApiKey completa a partir de um ApiKeyScope
    /// - Exemplo: scope.ApiKey.ClientName retorna o nome do cliente
    /// - O "= null!" e para satisfazer o compilador (nullable reference types)
    /// </remarks>
    public ApiKey ApiKey { get; set; } = null!;

    /// <summary>
    /// Nome do escopo/permissao.
    /// </summary>
    /// <value>Valores comuns: "read", "write", "delete"</value>
    /// <remarks>
    /// JUNIOR: Escopos comuns em APIs REST:
    /// - "read": permite buscar/listar dados (GET)
    /// - "write": permite criar/atualizar dados (POST, PUT, PATCH)
    /// - "delete": permite remover dados (DELETE)
    /// - "admin": acesso total (combina todos os anteriores)
    /// - "guest": acesso limitado (apenas leitura publica)
    /// </remarks>
    public string Scope { get; set; } = string.Empty;
}
