using Microsoft.AspNetCore.Mvc;
using SouthernTravels.Insights.Interfaces;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Controllers;

/// <summary>Ticket &amp; Service Status: tickets with their services, ops status and the Show all link. Filter lists come from the API.</summary>
[Route("Operations/TicketServiceStatus")]
public class TicketServiceStatusController : BaseController
{
    private readonly IOperationService _operationService;

    public TicketServiceStatusController(IOperationService operationService)
    {
        _operationService = operationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        // Filter dropdowns from Reports/GetOperationsReportFilters (the built-in lists if the API fails).
        var result = await _operationService.GetOperationsReportFiltersAsync();
        var filters = CommonFilters.TicketServiceFrom(result.Success ? result.Data : null);

        return View(TicketServiceStatusReport.Build(Q, filters));
    }
}
