/*
 * JUNIOR: ReportsController - Exemplo de Múltiplas Roles
 * =======================================================
 *
 * Este controller demonstra como permitir ACESSO A MÚLTIPLAS ROLES.
 *
 * SINTAXE: [Authorize(Roles = "Admin,Manager")]
 * ---------------------------------------------
 * A vírgula significa "OU" lógico.
 * Tradução: "Admin OU Manager podem acessar"
 *
 * EQUIVALÊNCIA:
 * [Authorize(Roles = "Admin,Manager")] é a mesma coisa que:
 *   if (User.IsInRole("Admin") || User.IsInRole("Manager")) { permite }
 *
 * PARA "E" (ambos precisam ter):
 * ---------------------------------------------
 * Não existe sintaxe direta para "E" com Roles.
 * Mas você pode usar Policy-based authorization:
 *
 * [Authorize(Policy = "AdminAndManager")]  // exige AMBOS
 *
 * E no Program.cs:
 * builder.Services.AddAuthorization(options =>
 * {
 *     options.AddPolicy("AdminAndManager", policy =>
 *         policy.RequireRole("Admin").RequireRole("Manager"));
 * });
 *
 * MAS CUIDADO: RequireRole com múltiplas chamadas é "E" para Policies,
 * mas quando você usa a sintaxe Roles="Admin,Manager" é "OU".
 *
 * HIERARQUIA DE ROLES:
 * --------------------
 * Neste sistema temos: Admin > Manager > User > Guest
 *
 * Se você é Manager, você TAMBÉM é User? Depende da implementação!
 * O código atual NÃO herda roles - cada role é independente.
 * Para herança, você precisaria de uma estrutura mais complexa.
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Rbac.Controllers;

/// <summary>
/// Controller de relatórios - acessível para Admin e Manager.
/// Demonstra uso de múltiplas roles com vírgula (OR lógico).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")] // JUNIOR: Admin OU Manager podem acessar
public class ReportsController : ControllerBase
{
    /// <summary>
    /// Lista relatórios - Admin e Manager.
    /// </summary>
    /// <returns>Mensagem de sucesso</returns>
    [HttpGet]
    public IActionResult GetReports()
    {
        // JUNIOR: Em apps reais, aqui você buscaria relatórios no banco
        // Filtraria por permissões específicas do usuário
        return Ok(new { message = "Relatórios - área de relatórios" });
    }

    /// <summary>
    /// Gera relatório financeiro - Admin e Manager.
    /// </summary>
    /// <returns>Mensagem de sucesso</returns>
    [HttpGet("financial")]
    public IActionResult GetFinancialReport()
    {
        // JUNIOR: Relatórios financeiros são sensíveis!
        // Além da role, em apps reais você adicionaria mais verificações:
        // - Claims ("podeVerFinanceiro" = true)
        // - Policy ("Apenas departamentos específicos")
        // - Auditoria (logar quem acessou)
        return Ok(new { message = "Relatório financeiro" });
    }
}
