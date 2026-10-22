using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>OPS User Workload: open services per operations user.</summary>
[Route("Operations/OpsUserWorkload")]
public class OpsUserWorkloadController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(OpsUserWorkloadReport.Build(Q));
}
