using Microsoft.AspNetCore.Mvc;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>The Operations area's front door and its error page.</summary>
[Route("Operations")]
public class HomeController : OperationsControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => Redirect(OperationsArea.Home);

    [Route("Error")]
    public IActionResult Error() => Problem();
}
