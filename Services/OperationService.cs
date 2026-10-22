using SouthernTravels.Insights.Common;
using SouthernTravels.Insights.Helpers;
using SouthernTravels.Insights.Interfaces;
using SouthernTravels.Insights.Models;
using SouthernTravels.Insights.Security;

namespace SouthernTravels.Insights.Services;

/// <summary>Operations report data from the API, written like STPLCostSheet's OperationService.</summary>
public class OperationService : BaseApiService, IOperationService
{
    private readonly IApiClientHelper _apiClient;
    private readonly ISessionSecurityService _security;
    private readonly IConfiguration _configuration;

    public OperationService(IApiClientHelper apiClient, ISessionSecurityService security, IConfiguration config) : base(config)
    {
        _apiClient = apiClient;
        _security = security;
        _configuration = config;
    }

    public async Task<ApiResponseViewModel> GetOperationsReportFiltersAsync()
    {
        var session = _security.ValidateSession();
        if (!session.IsValid) return Fail(session.ErrorMessage ?? "Session expired. Please login again.");

        try
        {
            var tenantId = int.TryParse(session.TenantId, out var t) && t > 0 ? t : _configuration.GetValue<int>("OperationSettings:TenantId");

            var response = await _apiClient.PostAsync<ApiResponseViewModel>(
                BuildUrl(ApiEndpoints.Reports, ApiEndpoints.ReportActions.GetOperationsReportFilters),
                new
                {
                    tenantID = tenantId,
                    userId = session.UserName,
                    userName = session.UserName,
                    securityKey = session.Token
                });

            return response ?? Fail("No response from the API.");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }
}
