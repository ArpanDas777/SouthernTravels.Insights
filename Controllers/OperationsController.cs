using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

public class OperationsController : Controller
{
    private ReportQuery Q => ReportQuery.From(Request.Query);

    /// <summary>
    /// Picking a report in the side menu links here with ?pick=1: remember that the rail should start collapsed
    /// (so the report gets the full width) and continue to the same page without the marker.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (Request.Query.ContainsKey("pick"))
        {
            Response.Cookies.Append("rail", "c", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
            context.Result = Redirect(Request.PathBase + Request.Path);
        }
        base.OnActionExecuting(context);
    }

    public IActionResult TicketServiceStatus() => View(TicketServiceStatusReport.Build(Q));

    public IActionResult OpsUserWorkload() => View(OpsUserWorkloadReport.Build(Q));

    public IActionResult ServiceWise() => View(ServiceWiseReport.Build(Q));

    public IActionResult VendorPayment() => View(VendorPaymentReport.Build(Q));

    public IActionResult Cancellation() => View(CancellationReport.Build(Q));

    public IActionResult Advance() => View(AdvanceReport.Build(Q));

    public IActionResult WeeklySalesTrend() => View(WeeklySalesTrendReport.Build(Q));

    public IActionResult DailyEmpSales() => View(DailyEmpSalesReport.Build(Q));

    public IActionResult SalesVsTarget() => View(SalesVsTargetReport.Build(Q));

    public IActionResult DutySlip() => View(DutySlipReport.Build(Q));

    public IActionResult Tcs() => View(TcsReport.Build(Q));

    public IActionResult Gst() => View(GstReport.Build(Q));

    public IActionResult Tds() => View(TdsReport.Build(Q));

    public IActionResult CashSheetAudit() => View(CashSheetAuditReport.Build(Q));

    public IActionResult BalancePending() => View(BalancePendingReport.Build(Q));

    public IActionResult Error() => Problem();
}
