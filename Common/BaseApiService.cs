using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Common;

/// <summary>Base of every API service (same as STPLCostSheet): reads the API URL from appsettings and builds "{ApiUrl}{controller}/{action}".</summary>
public abstract class BaseApiService
{
    protected readonly string BaseUrl;

    protected BaseApiService(IConfiguration config)
    {
        BaseUrl = config["STPLCostSheetAPI:ApiUrl"] ?? "";
        if (string.IsNullOrEmpty(BaseUrl))
            throw new Exception("API BaseUrl missing in configuration");
    }

    protected string BuildUrl(string controller, string action) => $"{BaseUrl}{controller}/{action}";

    /// <summary>Creates a common failed ApiResponseViewModel.</summary>
    protected ApiResponseViewModel Fail(string message) => new() { Success = false, Message = message };
}
