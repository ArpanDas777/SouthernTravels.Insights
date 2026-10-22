namespace SouthernTravels.Insights.Models;

/* =====================================================================
   TicketServiceStatus API (TicketServiceStatus/...): requests
   Property names match the API's RequestDTO.
===================================================================== */

/// <summary>Sent with every TicketServiceStatus call (filled from the session by OperationService).</summary>
public class TicketVendorWiseBaseRequest
{
    public string SecurityKey { get; set; } = "";
    public string UserName { get; set; } = "";
}

/// <summary>Filters shared by: Ticket list, Ticket KPI cards, Vendor list, Vendor sub list.</summary>
public class TicketWiseFilterRequest : TicketVendorWiseBaseRequest
{
    public string SearchText { get; set; } = "";        // MBK No, Ticket No, Passenger, File No (+ Vendor Name / Code on the vendor tab)
    public int? TicketStatusId { get; set; } = 0;       // 6 Confirmed, 7 Cancelled
    public int? OpsStatusId { get; set; } = 0;          // 1 New, 9 In Progress, 4 Completed
    public string BookingExecutive { get; set; } = "";
    public string BookingBranch { get; set; } = "";
    public string OpsUser { get; set; } = "";
    public string OpsBranch { get; set; } = "";
    public string ServiceType { get; set; } = "";       // ACCOMMODATION | TRANSPORT | GUIDE | ENROUTE | OTHER
    public int? ServiceCityId { get; set; } = 0;
    public string ServiceCity { get; set; } = "";
    public int? ServiceStatusId { get; set; } = 0;      // 2 Pending, 6 Confirmed, 7 Cancelled
    public int? VendorId { get; set; } = 0;
    public DateTime? BookingDateFrom { get; set; }
    public DateTime? BookingDateTo { get; set; }
    public DateTime? TourStartFrom { get; set; }
    public DateTime? TourStartTo { get; set; }
    public DateTime? ServiceStartFrom { get; set; }
    public DateTime? ServiceStartTo { get; set; }
    public DateTime? ConfirmationFrom { get; set; }
    public DateTime? ConfirmationTo { get; set; }
    public DateTime? PeriodFrom { get; set; }           // Period selector (applied on Booking Date)
    public DateTime? PeriodTo { get; set; }
    public bool MineOnly { get; set; }                  // "Files assigned to you"
}

/// <summary>Ticket list and Vendor list.</summary>
public class TicketWiseListRequest : TicketWiseFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Ticket-wise sub list (expanded ticket).</summary>
public class TicketWiseServicesRequest : TicketVendorWiseBaseRequest
{
    public int? TicketRowKey { get; set; }
    public long? MbkId { get; set; }
    public string TicketNo { get; set; } = "";
    public string SearchText { get; set; } = "";
    public bool ApplyFilters { get; set; } = true;
    public string OpsUser { get; set; } = "";
    public string OpsBranch { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public int? ServiceCityId { get; set; } = 0;
    public string ServiceCity { get; set; } = "";
    public int? ServiceStatusId { get; set; } = 0;
    public int? VendorId { get; set; } = 0;
    public DateTime? ServiceStartFrom { get; set; }
    public DateTime? ServiceStartTo { get; set; }
    public DateTime? ConfirmationFrom { get; set; }
    public DateTime? ConfirmationTo { get; set; }
}

/// <summary>Vendor-wise sub list: same filters as the list + the vendor that was expanded.</summary>
public class VendorWiseServicesRequest : TicketWiseFilterRequest
{
    public int ExpandedVendorId { get; set; }
}

/* =====================================================================
   Responses (the "data" of each call)
===================================================================== */

/// <summary>"Showing 1-10 of N".</summary>
public class TicketVendorPagingResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int ShowingFrom { get; set; }
    public int ShowingTo { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>GetTicketWiseList / GetVendorWiseList: { rows, paging }.</summary>
public class ListData<TRow>
{
    public List<TRow> Rows { get; set; } = [];
    public TicketVendorPagingResponse? Paging { get; set; }
}

public class TicketWiseListRowResponse
{
    public int TicketRowKey { get; set; }
    public int CostsheetID { get; set; }
    public long MBKID { get; set; }
    public string? MBKNo { get; set; }
    public string? TicketNo { get; set; }
    public string? FileNo { get; set; }
    public string? Passenger { get; set; }
    public string? TourName { get; set; }
    public int? Pax { get; set; }
    public int? NoOfAdult { get; set; }
    public int? NoOfChild { get; set; }
    public int? NoOfInfant { get; set; }
    public int? TicketStatusId { get; set; }
    public string? TicketStatus { get; set; }
    public bool? IsTicketCancelled { get; set; }
    public DateTime? CancelledOn { get; set; }
    public DateTime? BookingDate { get; set; }
    public string? BookingExecutiveID { get; set; }
    public string? BookingExecutiveName { get; set; }
    public string? BookingBranch { get; set; }
    public string? ExecutiveBranchId { get; set; }
    public int? OpsStatusId { get; set; }
    public string? OpsStatus { get; set; }
    public string? OpsBranchCodes { get; set; }
    public string? AssignedExecutives { get; set; }
    public DateTime? TourStartDate { get; set; }
    public DateTime? TourEndDate { get; set; }
    public int? TourDays { get; set; }
    public int? DaysLeft { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalancePending { get; set; }
    public decimal PercentPaid { get; set; }
    public int PaymentCount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public int ServiceCount { get; set; }
}

/// <summary>GetTicketWiseServices rows (expanded ticket).</summary>
public class TicketWiseServiceRowResponse
{
    public long ServiceRowId { get; set; }
    public long MBKID { get; set; }
    public string? TicketNo { get; set; }
    public int? CostsheetServiceID { get; set; }
    public long? ItinID { get; set; }
    public string? ServiceTypeCode { get; set; }
    public string? SvcType { get; set; }
    public string? City { get; set; }
    public int? CityId { get; set; }
    public string? NameOfService { get; set; }
    public string? Category { get; set; }
    public string? MealPlan { get; set; }
    public string? RoomSummary { get; set; }
    public int? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? OpsUserCodes { get; set; }
    public string? OpsUser { get; set; }
    public string? OpsBranch { get; set; }
    public int? OpsStatusId { get; set; }
    public string? OpsStatus { get; set; }
    public int? ServiceStatusId { get; set; }
    public string? ServiceStatus { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public string? ConfirmationNo { get; set; }
    public DateTime? SvcStart { get; set; }
    public DateTime? SvcEnd { get; set; }
    public int? Days { get; set; }
    public bool IsVoucherEnabled { get; set; }
}

/// <summary>GetTicketWiseKpi (the 5 cards; also used by the Vendor tab).</summary>
public class TicketWiseKpiResponse
{
    public int TotalTickets { get; set; }
    public int NewTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int CompletedTickets { get; set; }
    public decimal NewToCompletedTATDays { get; set; }
}

/// <summary>GetVendorWiseList rows.</summary>
public class VendorWiseListRowResponse
{
    public int VendorId { get; set; }
    public string? VendorCode { get; set; }
    public string? VendorName { get; set; }
    public string? Cities { get; set; }
    public string? ServiceTypeCodes { get; set; }
    public string? ServiceTypes { get; set; }
    public int Tickets { get; set; }
    public int ServiceCount { get; set; }
    public decimal TotalSaleValue { get; set; }
    public decimal VendorPayable { get; set; }
    public decimal VendorPaid { get; set; }
    public decimal BalancePending { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>GetVendorWiseServices rows (expanded vendor).</summary>
public class VendorWiseServiceRowResponse
{
    public long ServiceRowId { get; set; }
    public int TicketRowKey { get; set; }
    public long MBKID { get; set; }
    public string? MBKNo { get; set; }
    public string? TicketNo { get; set; }
    public string? FileNo { get; set; }
    public string? Passenger { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal PaidPercent { get; set; }
    public decimal DuePercent { get; set; }
    public decimal? PaymentsTotal { get; set; }
    public int PaymentCount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public string? ServiceTypeCode { get; set; }
    public string? SvcType { get; set; }
    public string? City { get; set; }
    public string? NameOfService { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Days { get; set; }
    public decimal? Selling { get; set; }
    public decimal? Payable { get; set; }
    public decimal? Profit { get; set; }
    public decimal? ProfitPercent { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public string? ConfirmationNo { get; set; }
    public string? OpsStatus { get; set; }
    public string? ServiceStatus { get; set; }
}
