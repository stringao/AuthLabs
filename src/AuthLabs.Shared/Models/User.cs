using Microsoft.AspNetCore.Identity;

namespace AuthLabs.Shared.Models;

/// <summary>
/// Representa um usuário no sistema de autenticação.
///
/// JUNIOR: Esta classe é o modelo base para usuários em TODOS os projetos
/// de autenticação desta solução (JWT, Cookie, Session, etc.).
///
/// Por que herdar de IdentityUser<int>?
/// ============================================================================
/// IdentityUser é uma classe do ASP.NET Identity que já vem com TODOS os campos
/// padrão que você esperaria em um usuário:
///   - Id (identificador único)
///   - UserName (nome de usuário para login)
///   - Email (endereço de email)
///   - PasswordHash (senha hasheada - NUNCA guarda senha em texto plain!)
///   - PhoneNumber (número de telefone opcional)
///   - LockoutEnd (para bloquear usuário após muitas tentativas erradas)
///   - E muitos outros campos úteis!
///
/// Ao herdar dela em vez de criar do zero, você ganha:
///   - Toda a infraestrutura de segurança já implementada
///   - Compatibilidade com UserManager e SignInManager
///   - Funcionalidades como confirmação de email, reset de senha, etc.
///
/// O <int> significa que o Id do usuário será um número inteiro.
/// (Você também pode usar string, GUID, etc. dependendo da sua necessidade)
/// </summary>
public class User : IdentityUser<int>
{
    /// <summary>
    /// Data e hora em que o usuário foi criado no sistema.
    ///
    /// JUNIOR: Por que Guardamos CreatedAt?
    /// - Auditoria: sabemos quando cada usuário foi cadastrado
    /// - Compliance: muitas leis exigem rastrear quando dados foram criados
    /// - Debugging: se houver problema, podemos ver a ordem dos cadastros
    /// - Analytics: podem calcular métricas como "usuários novos por dia"
    ///
    /// Por que DateTime.UtcNow?
    /// - UtcNow = Universal Coordinate Time (mesmo fuso horário para todos)
    /// - Evita problemas de horário de verão e fusos horários diferentes
    /// - Se você存取 de outro país, a data/hora será consistente
    /// - "Utc" significa "Coordinated Universal Time" (não "Universal")
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // =========================================================================
    // JUNIOR: Aqui você pode adicionar mais campos específicos da sua aplicação!
    //
    // Exemplos de campos comuns que você pode precisar:
    //
    //   public string FullName { get; set; } = string.Empty;
    //   public string? ProfilePictureUrl { get; set; }
    //   public DateTime? DateOfBirth { get; set; }
    //   public bool IsActive { get; set; } = true;
    //   public DateTime? LastLoginAt { get; set; }
    //
    // LEMBRE-SE: Cada campo que você adicionar aqui也需要:
    // 1. Adicionar a migration no banco de dados (dotnet ef migrations add)
    // 2. Atualizar o AppDbContext se precisar de configurações especiais
    // =========================================================================
}
