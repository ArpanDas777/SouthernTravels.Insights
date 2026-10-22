using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>GST Report.</summary>
[Route("Operations/Gst")]
public class GstController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(GstReport.Build(Q));
}
