namespace SouthernTravels.Insights.Models;

/// <summary>
/// The reply of every API call (like STPLCostSheet's ApiResponseViewModel): Success, Message and the data of that call.
/// Account/ValidateUserOTP fills objOTPLogin; Reports/GetOperationsReportFilters fills Data.
/// </summary>
public class ApiResponseViewModel
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public LoginUser? objOTPLogin { get; set; }
    public OperationsReportFilters? Data { get; set; }
}
