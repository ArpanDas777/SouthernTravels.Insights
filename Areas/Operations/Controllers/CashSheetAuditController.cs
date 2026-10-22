using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Cash Sheet Audit Report.</summary>
[Route("Operations/CashSheetAudit")]
public class CashSheetAuditController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(CashSheetAuditReport.Build(Q));
}
