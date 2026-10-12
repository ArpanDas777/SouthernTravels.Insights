using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Reports;

public sealed record PaymentVendorGroup(string Id, List<ServiceRow> Items)
{
    public string Name => SampleData.VendorName(Id);
    public IEnumerable<string> Types => Items.Select(x => x.S.Type).Distinct();
    public string Cities => string.Join(", ", Items.Select(x => x.S.City).Distinct());
    public int Days => Items.Sum(x => x.S.Days);
    public int Pax => ServiceRow.PaxOf(Items);
    public int ServiceCount => Items.Count;
    public double Payable => Items.Sum(x => x.S.Payable);
}

public sealed record PaymentTicketGroup(Ticket T, List<ServiceRow> Items)
{
    public string OpsUsers => string.Join(", ", T.Services.Select(s => s.Ops).Distinct());
    public string OpsBranches => string.Join(" / ", T.Services.Select(s => s.OpsBranch).Distinct());
    public double Payable => Items.Sum(x => x.S.Payable);
}

public sealed class VendorPaymentPage : ReportPage
{
    public bool IsVendorView => ActiveTab == "vendor";
    public List<PaymentVendorGroup> VendorRows { get; set; } = new();
    public List<PaymentTicketGroup> TicketRows { get; set; } = new();
}

public static class VendorPaymentReport
{
    private static readonly TabDef[] Tabs = { new("vendor", "Vendor Wise"), new("ticket", "Ticket Wise") };

    private static readonly FilterDef[] Filters =
    {
        new("Service Type", CommonFilters.ServiceTypes),
        new("Service City", CommonFilters.Cities),
        new("Ticket Status", new[] { "Confirmed", "Cancelled" }),
        new("Meal Plan", new[] { "EP", "CP", "MAP" }),
        new("OPS Status", new[] { "New", "In Progress", "Completed" }),
        new("Booking Executive", CommonFilters.Execs),
        new("Booking Branch", CommonFilters.Branches),
    };

    public static VendorPaymentPage Build(ReportQuery q)
    {
        var page = new VendorPaymentPage
        {
            Id = "r4",
            Title = "Vendor Payment details",
            Description = "Vendor Payment details — consolidated view of amounts owed to each vendor, grouped by vendor or by ticket, showing the net vendor payable per ticket and service. Gives OPS a clear picture of financial commitments to vendors.",
            Query = q,
            SearchPlaceholder = "Search Vendor Name, Ticket No., MBK No., Passenger, File No...",
            PeriodField = "Service Start Date",
            Tabs = Tabs,
            ActiveTab = ReportPage.ResolveTab(q, Tabs),
            Filters = Filters,
            DateFields = new[] { "Service Date Range (overlap)" },
        };

        var f = ServiceRow.All();

        if (page.IsVendorView)
        {
            var rows = f.GroupBy(x => x.S.VendorId).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new PaymentVendorGroup(g.Key, g.ToList())).ToList();
            (page.VendorRows, page.Pager) = Pager.Paginate(rows, q.Page);
            page.ResultLabel = rows.Count + " vendors";
        }
        else
        {
            var rows = f.GroupBy(x => x.T.No).Select(g => new PaymentTicketGroup(g.First().T, g.ToList())).ToList();
            (page.TicketRows, page.Pager) = Pager.Paginate(rows, q.Page);
            page.ResultLabel = rows.Count + " tickets";
        }
        return page;
    }
}
