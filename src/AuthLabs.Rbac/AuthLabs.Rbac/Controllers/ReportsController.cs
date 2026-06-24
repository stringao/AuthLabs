using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthLabs.Rbac.Controllers;

/// <summary>
/// Controller de relatórios - acessível para Admin e Manager.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : ControllerBase
{
    /// <summary>
    /// Lista relatórios - Admin e Manager.
    /// </summary>
    [HttpGet]
    public IActionResult GetReports()
    {
        return Ok(new { message = "Relatórios - área de relatórios" });
    }

    /// <summary>
    /// Gera relatório financeiro - Admin e Manager.
    /// </summary>
    [HttpGet("financial")]
    public IActionResult GetFinancialReport()
    {
        return Ok(new { message = "Relatório financeiro" });
    }
}
