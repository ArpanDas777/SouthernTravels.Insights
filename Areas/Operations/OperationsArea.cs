using SouthernTravels.Core.Controllers;
using SouthernTravels.Core.Models;

namespace SouthernTravels.Areas.Operations;

/// <summary>How the Operations area presents itself in the shared shell: heading, side menu (one entry per report) and home page.</summary>
public static class OperationsArea
{
    public const string Name = "Operations", Home = "/Operations/TicketServiceStatus";

    public static readonly AreaInfo Info = new("Reports", "REP", Home,
    [
        new("r1", "Ticket & Service Status", "M3 9a2 2 0 0 0 0 6v3a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1v-3a2 2 0 0 1 0-6V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1zM13 5v2M13 11v2M13 17v2", "/Operations/TicketServiceStatus"),
        new("r2", "OPS User Workload", "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75", "/Operations/OpsUserWorkload"),
        new("r3", "Service wise Report", "M12 2 2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5", "/Operations/ServiceWise"),
        new("r4", "Vendor Payment details", "M3 10l9-6 9 6M5 10v8M9 10v8M15 10v8M19 10v8M3 20h18", "/Operations/VendorPayment"),
        new("r5", "Cancellation Report", "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20zM15 9l-6 6M9 9l6 6", "/Operations/Cancellation"),
        new("r6", "Advance Report", "M2 7h20v10H2zM12 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM6 12h.01M18 12h.01", "/Operations/Advance"),
        new("r7", "Weekly Sales trend", "M3 17l6-6 4 4 8-8M15 7h6v6", "/Operations/WeeklySalesTrend"),
        new("r8", "Daily EMP Sales", "M8 2v4M16 2v4M3 10h18M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2zM8 18v-3M12 18v-5M16 18v-7", "/Operations/DailyEmpSales"),
        new("r9", "Sales vs Target", "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20zM12 18a6 6 0 1 0 0-12 6 6 0 0 0 0 12zM12 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4z", "/Operations/SalesVsTarget"),
        new("r10", "Duty Slip Report", "M5 17H3v-5l2-5h12l3 4 2 1v5h-2M5 17a2 2 0 1 0 4 0 2 2 0 0 0-4 0M9 17h6M15 17a2 2 0 1 0 4 0 2 2 0 0 0-4 0M8 7l-1 5h12M12 7v5", "/Operations/DutySlip"),
        new("r11", "TCS Report", "M4 3h16v18l-3-2-2 2-3-2-3 2-3-2-2 2zM12 8v6M9 11h6", "/Operations/Tcs"),
        new("r12", "GST Report", "M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9zM14 3v6h6M9.5 17.5l5-5M10 13h.01M14 17h.01", "/Operations/Gst"),
        new("r13", "TDS Report", "M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9zM14 3v6h6M9 15h6", "/Operations/Tds"),
        new("r14", "Cash Sheet Audit Report", "M20 12V8H6a2 2 0 0 1 0-4h12v4M4 6v12a2 2 0 0 0 2 2h14v-4M18 12a2 2 0 0 0 0 4h4v-4z", "/Operations/CashSheetAudit"),
        new("r15", "Balance Pending Report", "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20zM12 6v6l4 2", "/Operations/BalancePending"),
    ]);
}
