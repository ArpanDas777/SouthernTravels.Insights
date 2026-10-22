using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Interfaces;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Service wise Report, with the Trial Balance ledger tab (?tab=tb). Filter lists come from the API.</summary>
[Route("Operations/ServiceWise")]
public class ServiceWiseController : BaseController
{
    private readonly IOperationService _operationService;

    public ServiceWiseController(IOperationService operationService)
    {
        _operationService = operationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        // The Trial Balance tab has no filter panel, so the API is only called on the other tabs.
        if (Q.Tab == "tb")
            return View(ServiceWiseReport.Build(Q));

        var result = await _operationService.GetOperationsReportFiltersAsync();
        var filters = CommonFilters.TicketServiceFrom(result.Success ? result.Data : null);

        return View(ServiceWiseReport.Build(Q, filters));
    }
}
