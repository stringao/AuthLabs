namespace AuthLabs.Shared.Models;

/// <summary>
/// Representa um token de atualização (Refresh Token) no sistema.
///
/// JUNIOR: O que é um Refresh Token?
/// ============================================================================
/// Imagine que você faz login em um site. O servidor te dá um "token de acesso"
/// (Access Token / JWT) que dura, digamos, 15 minutos.
///
/// Após 15 minutos, esse token expira e você precisaria fazer login novamente.
/// Isso seria horrível para a experiência do usuário!
///
/// A solução é o Refresh Token:
///   - Você recebe DOIS tokens ao fazer login:
///     1. Access Token (JWT): curta duração (15 min), usado para API requests
///     2. Refresh Token: longa duração (7 dias), usado para renovar o Access Token
///   - Quando o Access Token expira, o frontend usa o Refresh Token
///     para pedir um novo Access Token SEM precisar de usuário/senha
///   - Se o Refresh Token também expirar, aí sim o usuário precisa fazer login
///
/// Vantagens deste fluxo:
///   - Segurança: Access Token curto reduz janela de ataque se vazar
///   - UX: Usuário não precisa fazer login constantemente
///   - Flexibilidade: Pode invalidar Refresh Tokens para fazer logout global
///
/// Este modelo é usado tanto no padrão JWT quanto no Cookie Auth.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Identificador único do refresh token no banco de dados.
    ///
    /// JUNIOR: Este é a chave primária (PK) da tabela.
    /// - Cada token tem um ID único
    /// - O banco usa isso para buscar rapidamente um token específico
    /// - É um int (número inteiro) auto-incrementado
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// A string do token em si (o valor que o cliente almacena).
    ///
    /// JUNIOR: Este é o "valor" do token que:
    ///   - O servidor gera criptograficamente quando o usuário faz login
    ///   - O cliente almacena (cookie, localStorage, etc.)
    ///   - O servidor valida quando o cliente envia para renovar o token
    ///
    /// Por que é unique (índice único)?
    ///   - Cada token deve ser usado apenas uma vez (após usar, gerar novo)
    ///   - Evita ataques de "replay" (malwares que capturam e reusam tokens)
    ///   - Garante que não há duplicatas no banco
    ///
    /// Por que é uma string (não um GUID)?
    ///   - Pode ser um JWT em si mesmo (contendo claims, expiração, etc.)
    ///   - Ou uma string aleatória criptograficamente segura
    ///   - Depende da implementação específica do projeto que usa esta library
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// ID do usuário que possui este refresh token.
    ///
    /// JUNIOR: Este campo conecta o token ao usuário.
    /// - Um usuário pode ter VÁRIOS refresh tokens (vários dispositivos)
    /// - Cada token está associado a exatamente um usuário
    /// - Esta é uma "Foreign Key" (chave estrangeira) para a tabela Users
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Data e hora em que este token expira e não pode mais ser usado.
    ///
    /// JUNIOR: Por que tokens expiram?
    ///   - Segurança: limita a janela de tempo que um token roubado é válido
    ///   - Compliance: algumas normas exigem expiração de credenciais
    ///   - Resource management: tokens expirados podem ser limpos do banco
    ///
    /// Valores típicos para expiração:
    ///   - Curto: 1 dia (mais seguro)
    ///   - Médio: 7 dias (bom equilíbrio)
    ///   - Longo: 30 dias (mais prático, menos seguro)
    ///
    /// Dica: Alguns sistemas usam "sliding expiration" - cada vez que o token
    /// é usado, a expiração é estendida. Isso é bom para UX mas pode ser
    /// um risco de segurança em caso de token roubado.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Data e hora em que este token foi criado.
    ///
    /// JUNIOR: Diferença de CreatedAt vs ExpiresAt:
    ///   - CreatedAt: quando o token foi gerado (para auditoria)
    ///   - ExpiresAt: quando o token para de funcionar
    ///
    /// O tempo de vida = ExpiresAt - CreatedAt
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indica se este token foi revogado/invalidado.
    ///
    /// JUNIOR: Por que temos um campo de "revogado"?
    ///   - Permite invalidar tokens específicos sem deletar do banco
    ///   - Útil quando: usuário faz logout, token é roubado, admin bloqueia
    ///   - Um token revogado NÃO pode ser usado para obter novo access token
    ///
    /// O que acontece quando um token é roubado?
    ///   1. Usuario reporta roubo → Admin marca IsRevoked = true
    ///   2. Atacante tenta usar o token → Servidor nega (já que está revogado)
    ///   3. Usuario faz login novamente → Recebe novo refresh token
    ///
    /// Por que não deletar o token ao invés de marcar como revogado?
    ///   - Auditoria: você ainda pode ver QUEM teve tokens, quando, etc.
    ///   - History:保留了 histórico para investigar incidentes de segurança
    ///   - Analytics: pode entender padrões de uso dos tokens
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Navegação para o usuário dono deste token.
    ///
    /// JUNIOR: O que é uma "Navigation Property"?
    ///   - Permite acessar os dados do usuário relacionado a partir do token
    ///   - Exemplo: refreshToken.User.UserName retorna o nome do usuário
    ///
    /// Esta é uma relação "muitos para um" (Many-to-One):
    ///   - UM usuário pode ter VÁRIOS refresh tokens (vários dispositivos)
    ///   - CADA refresh token pertence a UM usuário
    ///
    /// O "= null!" é uma forma de dizer ao compilador:
    ///   "Eu garanto que este valor não será null quando usado"
    ///   (É como um "suppress warnings" para nullable reference types)
    /// </summary>
    public User User { get; set; } = null!;

    // =========================================================================
    // JUNIOR: Campos opcionais que você pode adicionar:
    //
    //   public string? DeviceInfo { get; set; }
    //   // Exemplo: "Chrome on Windows 10" ou "iPhone 12"
    //
    //   public string? IpAddress { get; set; }
    //   // Para auditoria: de onde veio a requisição
    //
    //   public DateTime? LastUsedAt { get; set; }
    //   // Quando o token foi usado pela última vez
    //
    // =========================================================================
}
