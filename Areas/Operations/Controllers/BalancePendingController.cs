using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Balance Pending Report.</summary>
[Route("Operations/BalancePending")]
public class BalancePendingController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(BalancePendingReport.Build(Q));
}
