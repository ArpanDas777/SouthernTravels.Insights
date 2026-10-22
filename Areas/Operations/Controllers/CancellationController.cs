using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Cancellation Report.</summary>
[Route("Operations/Cancellation")]
public class CancellationController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(CancellationReport.Build(Q));
}
