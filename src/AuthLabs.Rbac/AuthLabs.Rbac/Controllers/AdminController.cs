/*
 * JUNIOR: AdminController - Exemplo de RBAC (Role-Based Access Control)
 * ======================================================================
 *
 * Este controller demonstra AUTORIZAÇÃO BASEADA EM ROLES (RBAC).
 *
 * O QUE É RBAC?
 * -------------
 * RBAC significa "Role-Based Access Control" (Controle de Acesso Baseado em Papéis).
 * É o modelo mais simples e comum de autorização.
 *
 * IDEIA CENTRAL: "Se você PERTENCE ao papel X, você PODE fazer Y"
 *
 * EXEMPLOS NO CÓDIGO:
 * - [Authorize(Roles = "Admin")] = só Admin pode acessar
 * - [Authorize(Roles = "Admin,Manager")] = Admin OU Manager pode acessar
 * - User.IsInRole("Admin") = retorna true se o usuário é Admin
 *
 * PRÓS DO RBAC:
 * + Simples de entender e implementar
 * + Bom para sistemas com poucos tipos de usuários
 * + Decisões de permissão são rápidas (verificar string)
 *
 * CONTRAS DO RBAC:
 * - Não escala bem se você tem muitas permissões granulares
 * - "Admin" pode significar coisas diferentes em contextos diferentes
 * - Não consegue expressar "pode fazer X mas não Y"
 *
 * QUANDO USAR RBAC vs CLAIMS:
 * - RBAC: bom para autorização coarse-grained (grosseira)
 *   Ex: "Admin pode tudo, User pode ver, Guest pode só ler"
 * - Claims: bom para autorização fine-grained (detalhada)
 *   Ex: "João pode editar relatórios do departamento Financeiro"
 *
 * NESTE PROJETO:
 * Usamos RBAC para roles principais (Admin, Manager, User, Guest)
 * E claims para informações do usuário (email, nome, etc.)
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Rbac.Controllers;

/// <summary>
/// Controller administrativo - acessível apenas para Admin.
/// Demonstra o uso de [Authorize(Roles = "Admin")] para RBAC.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // JUNIOR: RBAC em ação! Só Admin entra aqui.
public class AdminController : ControllerBase
{
    /// <summary>
    /// Lista todos os usuários - apenas Admin.
    /// </summary>
    /// <returns>Mensagem de sucesso</returns>
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        // JUNIOR: Em apps reais, aqui você buscaria usuários no banco
        // e retornaria uma lista de usuários (sem senha, claro!)
        return Ok(new { message = "Lista de usuários - área administrativa" });
    }

    /// <summary>
    /// Dashboard administrativo - apenas Admin.
    /// </summary>
    /// <returns>Mensagem de sucesso</returns>
    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        // JUNIOR: Em apps reais, aqui você retornaria métricas,
        // estatísticas, logs do sistema, etc.
        return Ok(new { message = "Dashboard administrativo" });
    }
}
