using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Vendor Payment details.</summary>
[Route("Operations/VendorPayment")]
public class VendorPaymentController : BaseController
{
    [HttpGet("")]
    public IActionResult Index() => View(VendorPaymentReport.Build(Q));
}
