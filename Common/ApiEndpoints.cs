namespace SouthernTravels.Insights.Common;

/// <summary>API controller and action names (same idea as STPLCostSheet's COMMON/ApiEndpoints.cs). URL = STPLCostSheetAPI:ApiUrl + controller + "/" + action.</summary>
public static class ApiEndpoints
{
    public const string Account = "Account";
    public const string Reports = "Reports";

    public static class AccountActions
    {
        public const string SendOTP = "SendOTP";
        public const string ValidateUserOTP = "ValidateUserOTP";
    }

    public static class ReportActions
    {
        public const string GetOperationsReportFilters = "GetOperationsReportFilters";
    }
}
