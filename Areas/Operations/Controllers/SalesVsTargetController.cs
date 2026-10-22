using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Sales vs Target.</summary>
[Route("Operations/SalesVsTarget")]
public class SalesVsTargetController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(SalesVsTargetReport.Build(Q));
}
