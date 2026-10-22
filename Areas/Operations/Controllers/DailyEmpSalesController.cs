using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Daily EMP Sales, with the Comparison view (?tab=cmp).</summary>
[Route("Operations/DailyEmpSales")]
public class DailyEmpSalesController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(DailyEmpSalesReport.Build(Q));
}
