using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Sales vs Target.</summary>
[Route("Operations/SalesVsTarget")]
public class SalesVsTargetController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(SalesVsTargetReport.Build(Q));
}
