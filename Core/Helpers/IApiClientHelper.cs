namespace SouthernTravels.Core.Helpers;

/// <summary>Sends a request to the backend API and reads the JSON reply (same contract as STPLCostSheet).</summary>
public interface IApiClientHelper
{
    /// <summary>POSTs <paramref name="requestObj"/> as JSON.</summary>
    Task<T?> PostAsync<T>(string url, object requestObj);

    /// <summary>GETs <paramref name="url"/> (for read-only endpoints that take no body).</summary>
    Task<T?> GetAsync<T>(string url);
}

/// <summary>An error reply from the API; <see cref="StatusCode"/> is its HTTP status (e.g. 405 when the endpoint expects another method).</summary>
public class ApiException(int statusCode, string message) : ApplicationException(message)
{
    public int StatusCode { get; } = statusCode;
}
