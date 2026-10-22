using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Weekly Sales trend.</summary>
[Route("Operations/WeeklySalesTrend")]
public class WeeklySalesTrendController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(WeeklySalesTrendReport.Build(Q));
}
