namespace SouthernTravels.Areas.Operations.Models.Api;

/// <summary>
/// Fields sent with every Operations API call, as SouthernTravels.Finance and STPLCostSheet do (tenantID, userId, userName, securityKey).
/// OperationsApiService fills them from the session.
/// </summary>
public class OperationsApiRequest
{
    public int TenantID { get; set; }
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string SecurityKey { get; set; } = "";
}
