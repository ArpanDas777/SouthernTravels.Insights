using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

public class OperationsController : Controller
{
    private ReportQuery Q => ReportQuery.From(Request.Query);

    public IActionResult TicketServiceStatus() => View(TicketServiceStatusReport.Build(Q));

    public IActionResult OpsUserWorkload() => View(OpsUserWorkloadReport.Build(Q));

    public IActionResult ServiceWise() => View(ServiceWiseReport.Build(Q));

    public IActionResult VendorPayment() => View(VendorPaymentReport.Build(Q));

    public IActionResult Cancellation() => View(CancellationReport.Build(Q));

    public IActionResult Advance() => View(AdvanceReport.Build(Q));

    public IActionResult WeeklySalesTrend() => View(WeeklySalesTrendReport.Build(Q));

    public IActionResult DailyEmpSales() => View(DailyEmpSalesReport.Build(Q));

    public IActionResult SalesVsTarget() => View(SalesVsTargetReport.Build(Q));

    public IActionResult Error() => Problem();
}
