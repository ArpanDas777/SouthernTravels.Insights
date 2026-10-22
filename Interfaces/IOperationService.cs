using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Interfaces;

/// <summary>Operations report data from the API.</summary>
public interface IOperationService
{
    /// <summary>Reports/GetOperationsReportFilters: the filter dropdown lists (in Data).</summary>
    Task<ApiResponseViewModel> GetOperationsReportFiltersAsync();
}
