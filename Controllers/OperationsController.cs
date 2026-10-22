using Microsoft.AspNetCore.Mvc;

namespace SouthernTravels.Insights.Controllers;

/// <summary>/Operations (goes to the first report) and the error page.</summary>
[Route("Operations")]
public class OperationsController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => Redirect("/Operations/TicketServiceStatus");

    [Route("Error")]
    public IActionResult Error() => Problem();
}
