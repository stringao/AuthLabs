/*
 * JUNIOR: RoleService - Implementação do Serviço de Papéis
 * =========================================================
 *
 * ATENÇÃO - NOTA IMPORTANTE:
 * Este serviço é um PASSTHROUGH (passagem) - ele apenas DELAGA
 * as chamadas para o UserManager do Identity.
 *
 * POR QUE EXISTE ESTE ARQUIVO?
 * ----------------------------
 * 1. ABSTRAÇÃO: Separa a lógica de negócio do ASP.NET Identity
 *    Se no futuro você quiser trocar Identity por outro sistema,
 *    só mudaria esta classe, não os controllers.
 *
 * 2. ENSINAMENTO: Demonstra como criar um serviço que encapsula
 *    operações de usuário/role. Útil para entender arquitetura.
 *
 * 3. FACILIDADE DE TESTE: Em testes unitários, você pode criar
 *    um mock do IRoleService sem precisar do Identity real.
 *
 * PODERIA SER REMOVIDO?
 * Sim! Você poderia chamar UserManager diretamente nos controllers.
 * Mas em projetos reais, ter esta camada de serviço é uma boa prática.
 *
 * EXEMPLO SEM O SERVICE (direto no Controller):
 * ----------------------------------------------
 * var user = await _userManager.FindByEmailAsync(email);
 * var roles = await _userManager.GetRolesAsync(user);
 *
 * EXEMPLO COM O SERVICE (como está agora):
 * ------------------------------------------
 * var user = await _roleService.GetUserByEmailAsync(email);
 * var roles = await _roleService.GetUserRolesAsync(user);
 *
 * A diferença é mínima neste caso, mas conforme a lógica cresce,
 * ter um serviço dedicado facilita manutenções futuras.
 */

using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthLabs.Rbac.Services;

/// <summary>
/// Implementação do serviço de gerenciamento de roles.
/// Este é um PASSTHROUGH - existe apenas para fins educacionais e abstração.
/// Em projetos menores, as chamadas poderiam ser feitas direto ao UserManager.
/// </summary>
public class RoleService : IRoleService
{
    // JUNIOR: UserManager é providedo pelo ASP.NET Identity
    // Ele oferece métodos para:
    // - Criar/deletar usuários
    // - Adicionar/remover roles
    // - Verificar senhas
    // - Buscar por email, username, ID
    private readonly UserManager<User> _userManager;

    public RoleService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Obtém usuário pelo email.
    /// </summary>
    /// <param name="email">Email do usuário a buscar</param>
    /// <returns>Usuário encontrado ou null se não existir</returns>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        // JUNIOR: FindByEmailAsync busca o usuário na tabela de usuários
        // Retorna null se não encontrar - por isso verificamos antes de usar
        return await _userManager.FindByEmailAsync(email);
    }

    /// <summary>
    /// Obtém todas as roles de um usuário.
    /// </summary>
    /// <param name="user">Usuário cujas roles serão buscadas</param>
    /// <returns>Lista de nomes de roles (ex: ["Admin", "User"])</returns>
    public async Task<IList<string>> GetUserRolesAsync(User user)
    {
        // JUNIOR: GetRolesAsync retorna as roles como IList<string>
        // Cada string é o nome da role ("Admin", "Manager", etc.)
        // Se o usuário não tem roles, retorna lista vazia (não null)
        return await _userManager.GetRolesAsync(user);
    }
}
