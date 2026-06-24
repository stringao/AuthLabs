/*
 * JUNIOR: IRoleService - Interface do Serviço de Papéis
 * ======================================================
 *
 * O QUE É UMA INTERFACE?
 * ----------------------
 * Uma interface define um "CONTRATO" - ela especifica QUAIS métodos
 * uma classe DEVE implementar, mas NÃO diz COMO implementá-los.
 *
 * SINTAXE: public interface IRoleService { ... }
 * - Interfaces começam com "I" por convenção em C# (.NET)
 * - Contém apenas assinatura dos métodos (sem implementação)
 *
 * PARA QUE SERVE?
 * ----------------
 * 1. INVERSÃO DE CONTROLE (IoC): Ao invés do controller criar o serviço,
 *    ele recebe via construtor. O ASP.NET injeta a implementação.
 *
 * 2. SUBSTITUIÇÃO: Você pode criar múltiplas implementações:
 *    - RoleService (usa Identity real)
 *    - MockRoleService (para testes - retorna dados fake)
 *    - ExternalRoleService (busca roles de outro sistema)
 *
 * 3. TESTES: Em testes unitários, você cria um mock da interface
 *    que retorna dados controlados, sem precisar de banco real.
 *
 * EXEMPLO DE USO:
 * ----------------
 * // No Controller:
 * private readonly IRoleService _roleService;
 * public AuthController(IRoleService roleService) {
 *     _roleService = roleService;
 * }
 *
 * // No Program.cs (injeção automática):
 * builder.Services.AddScoped<IRoleService, RoleService>();
 *
 * QUANDO USAR INTERFACES?
 * ------------------------
 * - Quando você sabe que a implementação pode mudar
 * - Quando você quer facilitar testes unitários
 * - Quando você quer separar "o que" do "como"
 *
 * NESTE PROJETO:
 * A interface existe principalmente para FINS EDUCACIONAIS.
 * Em um projeto tão simples, poderíamos usar RoleService direto.
 * Mas em projetos maiores, interfaces são essenciais.
 */

using AuthLabs.Shared.Models;

namespace AuthLabs.Rbac.Services;

/// <summary>
/// Interface para serviço de gerenciamento de roles.
/// Define o contrato para operações de busca de usuários e roles.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Obtém usuário pelo email.
    /// </summary>
    /// <param name="email">Email do usuário a buscar</param>
    /// <returns>Usuário encontrado ou null</returns>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Obtém as roles de um usuário.
    /// </summary>
    /// <param name="user">Usuário cujas roles serão obtidas</param>
    /// <returns>Lista de nomes de roles</returns>
    Task<IList<string>> GetUserRolesAsync(User user);
}
