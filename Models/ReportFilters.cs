namespace SouthernTravels.Insights.Models;

/// <summary>Data of Reports/GetOperationsReportFilters: the dropdown lists used by the report filters.</summary>
public class OperationsReportFilters
{
    public List<StatusItem> TicketStatus { get; set; } = [];
    public List<StatusItem> OpsStatus { get; set; } = [];
    public List<StatusItem> ServiceStatus { get; set; } = [];
    public List<OpsUserItem> OpsUser { get; set; } = [];
    public List<OpsBranchItem> OpsBranch { get; set; } = [];
    public List<BookingExecutiveItem> BookingExecutive { get; set; } = [];
    public List<BookingBranchItem> BookingBranch { get; set; } = [];
    public List<ServiceCityItem> ServiceCity { get; set; } = [];
    public List<VendorItem> Vendor { get; set; } = [];
}

public class StatusItem
{
    public int StatusID { get; set; }
    public string? StatusName { get; set; }
}

public class OpsUserItem
{
    public string? ExecutivesId { get; set; }
    public string? Name { get; set; }
}

public class OpsBranchItem
{
    public string? OpsBranch { get; set; }
}

public class BookingExecutiveItem
{
    public string? BranchCode { get; set; }
    public string? UserName { get; set; }
}

public class BookingBranchItem
{
    public string? BranchCode { get; set; }
    public string? BranchName { get; set; }
}

public class ServiceCityItem
{
    public int CityID { get; set; }
    public string? CityName { get; set; }
}

public class VendorItem
{
    public int HotelID { get; set; }
    public string? HotelName { get; set; }
}
