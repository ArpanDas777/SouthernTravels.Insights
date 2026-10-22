using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Duty Slip Report.</summary>
[Route("Operations/DutySlip")]
public class DutySlipController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(DutySlipReport.Build(Q));
}
