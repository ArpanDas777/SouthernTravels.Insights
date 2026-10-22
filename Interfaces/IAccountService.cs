using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Interfaces;

/// <summary>OTP login against the Account API.</summary>
public interface IAccountService
{
    Task<ApiResponseViewModel> SendOtpAsync(EmailIdRequest request);
    Task<ApiResponseViewModel> ValidateOtpAsync(EmailLoginRequest request);
}
