using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Weekly Sales trend.</summary>
[Route("Operations/WeeklySalesTrend")]
public class WeeklySalesTrendController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(WeeklySalesTrendReport.Build(Q));
}
