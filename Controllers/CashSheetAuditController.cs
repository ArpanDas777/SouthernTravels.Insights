using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Cash Sheet Audit Report.</summary>
[Route("Operations/CashSheetAudit")]
public class CashSheetAuditController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(CashSheetAuditReport.Build(Q));
}
