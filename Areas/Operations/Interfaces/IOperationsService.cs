using SouthernTravels.Areas.Operations.Models.Api;

namespace SouthernTravels.Areas.Operations.Interfaces;

/// <summary>Everything the Operations controllers read from the backend API (<see cref="Services.OperationsApiService"/>).</summary>
public interface IOperationsService
{
    /// <summary>The filter dropdown lists (Reports/GetOperationsReportFilters), or null when the API could not be reached.</summary>
    Task<OperationsReportFilters?> GetReportFiltersAsync();
}
