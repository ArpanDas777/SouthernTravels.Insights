using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>TDS Report.</summary>
[Route("Operations/Tds")]
public class TdsController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(TdsReport.Build(Q));
}
