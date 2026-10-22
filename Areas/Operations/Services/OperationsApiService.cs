using Microsoft.Extensions.Caching.Memory;
using SouthernTravels.Areas.Operations.Common;
using SouthernTravels.Areas.Operations.Interfaces;
using SouthernTravels.Areas.Operations.Models.Api;
using SouthernTravels.Core.Common;
using SouthernTravels.Core.Helpers;
using SouthernTravels.Core.Security;

namespace SouthernTravels.Areas.Operations.Services;

/// <summary>
/// Operations data from the backend API, built like SouthernTravels.Finance's FinanceApiService: BaseApiService for the URL,
/// IApiClientHelper for the call, and the session's user and token sent with every request.
/// </summary>
public class OperationsApiService : BaseApiService, IOperationsService
{
    readonly IApiClientHelper _apiClient;
    readonly ISessionSecurityService _security;
    readonly IConfiguration _configuration;
    readonly IMemoryCache _cache;
    readonly ILogger<OperationsApiService> _logger;

    public OperationsApiService(IApiClientHelper apiClient, ISessionSecurityService security, IConfiguration config,
        IMemoryCache cache, ILogger<OperationsApiService> logger) : base(config, "OperationsAPI")
    {
        _apiClient = apiClient;
        _security = security;
        _configuration = config;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Fills tenant, user and security key from the session (as every STPLCostSheet call does).</summary>
    T Signed<T>(T request) where T : OperationsApiRequest
    {
        var session = _security.ValidateSession();
        if (!session.IsValid) throw new ApplicationException(session.ErrorMessage ?? "Session expired. Please login again.");
        request.TenantID = int.TryParse(session.TenantId, out var tid) && tid > 0 ? tid : _configuration.GetValue("OperationsSettings:TenantId", 1);
        request.UserId = session.UserName!;
        request.UserName = session.UserName!;
        request.SecurityKey = session.Token!;
        return request;
    }

    /// <summary>
    /// POSTs the signed request, like every other STPLCostSheet / Finance call. If the endpoint only accepts GET (405), it is called with GET.
    /// </summary>
    async Task<T?> Call<T>(string controller, string action, OperationsApiRequest request)
    {
        var url = BuildUrl(controller, action);
        try
        {
            return await _apiClient.PostAsync<T>(url, Signed(request));
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status405MethodNotAllowed)
        {
            return await _apiClient.GetAsync<T>(url);
        }
    }

    public async Task<OperationsReportFilters?> GetReportFiltersAsync()
    {
        // The lists change rarely (thousands of cities and vendors), so they are kept in memory per tenant for a while.
        var key = "ops-filters:" + (_security.ValidateSession().TenantId ?? "");
        if (_cache.TryGetValue(key, out OperationsReportFilters? cached)) return cached;

        try
        {
            var response = await Call<OperationsFilterResponse>(OperationsApiEndpoints.Reports,
                OperationsApiEndpoints.ReportsActions.GetOperationsReportFilters, new OperationsApiRequest());
            if (response is not { Success: true, Data: { } data })
            {
                _logger.LogWarning("Operations API GetOperationsReportFilters returned no data: {Message}", response?.Message);
                return null;
            }

            _cache.Set(key, data, TimeSpan.FromMinutes(_configuration.GetValue("OperationsSettings:FiltersCacheMinutes", 30)));
            return data;
        }
        catch (ApplicationException ex)
        {
            // API down or session expired: the page still opens, with the built-in filter lists.
            _logger.LogError(ex, "Operations API GetOperationsReportFilters failed");
            return null;
        }
    }
}
