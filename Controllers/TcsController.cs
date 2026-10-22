using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>TCS Report.</summary>
[Route("Operations/Tcs")]
public class TcsController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(TcsReport.Build(Q));
}
