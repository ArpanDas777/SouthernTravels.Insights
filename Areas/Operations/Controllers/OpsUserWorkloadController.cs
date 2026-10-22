using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>OPS User Workload: open services per operations user.</summary>
[Route("Operations/OpsUserWorkload")]
public class OpsUserWorkloadController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(OpsUserWorkloadReport.Build(Q));
}
