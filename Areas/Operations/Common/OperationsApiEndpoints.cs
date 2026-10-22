namespace SouthernTravels.Areas.Operations.Common;

/// <summary>Operations API endpoints. URL = OperationsAPI:ApiUrl + controller + "/" + action.</summary>
public static class OperationsApiEndpoints
{
    public const string Reports = "Reports";

    public static class ReportsActions
    {
        /// <summary>Filter dropdown lists: ticket/OPS/service status, OPS users and branches, booking executives and branches, service cities, vendors.</summary>
        public const string GetOperationsReportFilters = "GetOperationsReportFilters";
    }
}
