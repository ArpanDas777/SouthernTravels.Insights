using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Daily EMP Sales, with the Comparison view (?tab=cmp).</summary>
[Route("Operations/DailyEmpSales")]
public class DailyEmpSalesController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(DailyEmpSalesReport.Build(Q));
}
