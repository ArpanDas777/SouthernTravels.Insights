using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>GST Report.</summary>
[Route("Operations/Gst")]
public class GstController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(GstReport.Build(Q));
}
