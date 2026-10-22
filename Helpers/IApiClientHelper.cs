namespace SouthernTravels.Insights.Helpers;

/// <summary>Posts a request object as JSON to the API and reads the JSON reply (same as STPLCostSheet).</summary>
public interface IApiClientHelper
{
    Task<T?> PostAsync<T>(string url, object requestObj);
}
