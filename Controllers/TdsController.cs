using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>TDS Report.</summary>
[Route("Operations/Tds")]
public class TdsController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(TdsReport.Build(Q));
}
