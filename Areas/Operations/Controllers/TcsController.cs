using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>TCS Report.</summary>
[Route("Operations/Tcs")]
public class TcsController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(TcsReport.Build(Q));
}
