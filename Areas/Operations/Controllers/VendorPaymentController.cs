using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Vendor Payment details.</summary>
[Route("Operations/VendorPayment")]
public class VendorPaymentController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View(VendorPaymentReport.Build(Q));
}
