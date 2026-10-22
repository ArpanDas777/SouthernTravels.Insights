using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Interfaces;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Ticket &amp; Service Status: tickets with their services, ops status and the Show all link. Filter lists come from the Operations API.</summary>
[Route("Operations/TicketServiceStatus")]
public class TicketServiceStatusController(IOperationsService operations) : OperationsControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var filters = CommonFilters.TicketServiceFrom(await operations.GetReportFiltersAsync());
        return View(TicketServiceStatusReport.Build(Q, filters));
    }
}
