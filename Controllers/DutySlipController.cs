using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Duty Slip Report.</summary>
[Route("Operations/DutySlip")]
public class DutySlipController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(DutySlipReport.Build(Q));
}
