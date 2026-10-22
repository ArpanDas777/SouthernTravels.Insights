using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Areas.Operations.Interfaces;
using SouthernTravels.Areas.Operations.Reports;

namespace SouthernTravels.Areas.Operations.Controllers;

/// <summary>Service wise Report, with the Trial Balance ledger tab (?tab=tb). Filter lists come from the Operations API.</summary>
[Route("Operations/ServiceWise")]
public class ServiceWiseController(IOperationsService operations) : OperationsControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        // The Trial Balance tab has no filter panel, so the lists are only needed on the other tabs.
        var filters = Q.Tab == "tb" ? null : CommonFilters.TicketServiceFrom(await operations.GetReportFiltersAsync());
        return View(ServiceWiseReport.Build(Q, filters));
    }
}
