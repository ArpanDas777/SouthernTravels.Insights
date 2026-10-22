using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Advance Report.</summary>
[Route("Operations/Advance")]
public class AdvanceController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(AdvanceReport.Build(Q));
}
