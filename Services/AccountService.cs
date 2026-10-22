using SouthernTravels.Insights.Common;
using SouthernTravels.Insights.Helpers;
using SouthernTravels.Insights.Interfaces;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Services;

/// <summary>Account/SendOTP and Account/ValidateUserOTP, like STPLCostSheet's SendOTPAsync / ValidateUserOTPAsync.</summary>
public class AccountService : BaseApiService, IAccountService
{
    private readonly IApiClientHelper _apiClient;

    public AccountService(IApiClientHelper apiClient, IConfiguration config) : base(config)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponseViewModel> SendOtpAsync(EmailIdRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponseViewModel>(
                BuildUrl(ApiEndpoints.Account, ApiEndpoints.AccountActions.SendOTP),
                new { request.EmailId, request.UserIP });
            return response ?? Fail("Failed to send OTP.");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<ApiResponseViewModel> ValidateOtpAsync(EmailLoginRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponseViewModel>(
                BuildUrl(ApiEndpoints.Account, ApiEndpoints.AccountActions.ValidateUserOTP),
                new { request.EmailId, request.OTPCode, request.UserIP });
            return response ?? Fail("OTP validation failed.");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }
}
