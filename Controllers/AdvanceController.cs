using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Advance Report.</summary>
[Route("Operations/Advance")]
public class AdvanceController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(AdvanceReport.Build(Q));
}
