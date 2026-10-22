using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Cancellation Report.</summary>
[Route("Operations/Cancellation")]
public class CancellationController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(CancellationReport.Build(Q));
}
