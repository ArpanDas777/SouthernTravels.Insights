using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Balance Pending Report.</summary>
[Route("Operations/BalancePending")]
public class BalancePendingController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(BalancePendingReport.Build(Q));
}
