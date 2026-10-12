using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

public sealed record ServiceGroup(string Key, List<ServiceRow> Items)
{
    public int Days => Items.Sum(x => x.S.Days);
    public double Sell => Items.Sum(x => x.S.Sell);
    public double Contracted => Items.Sum(x => x.Contracted);
    public double Negotiated => Items.Sum(x => x.Negotiated);
    public double Variation => Items.Sum(x => x.Variation);
    public int Vendors => Items.Select(x => x.S.VendorId).Distinct().Count();
    public int Pax => ServiceRow.PaxOf(Items);
}

public sealed class ServiceWisePage : ReportPage
{
    public static readonly IReadOnlyDictionary<string, string> StateOf = new Dictionary<string, string>
    {
        ["Agra"] = "Uttar Pradesh", ["Delhi"] = "Delhi", ["Hyderabad"] = "Telangana", ["Jaipur"] = "Rajasthan",
    };

    public bool IsVendorView => ActiveTab == "vendor";
    public bool IsTrialBalance => ActiveTab == "tb";
    public TrialBalance? Tb { get; set; }
    public List<ServiceRow> Rows { get; set; } = new();
    public List<ServiceGroup> Groups { get; set; } = new();
}

public static class ServiceWiseReport
{
    private static readonly TabDef[] Tabs = { new("vendor", "Vendor Wise"), new("city", "Service City-wise"), new("tb", "Trial Balance") };

    public static ServiceWisePage Build(ReportQuery q)
    {
        var tab = ReportPage.ResolveTab(q, Tabs);
        var page = new ServiceWisePage
        {
            Id = "r3",
            Title = "Service wise Report",
            Description = "Service wise Report — cross-sectional view of all services grouped by Vendor, Service City, and Service Type. Primary tool for destination-level planning, vendor management, and on-ground operational coordination.",
            Query = q,
            SearchPlaceholder = "Search Ticket No., Vendor Name, Name of Service...",
            PeriodField = tab == "tb" ? "" : "Service Start Date",
            Tabs = Tabs,
            ActiveTab = tab,
            ToolbarExtra = tab == "tb" ? "_TrialBalanceControls" : null,
            HasExpand = tab != "tb",
            Filters = CommonFilters.TicketService,
            DateFields = CommonFilters.TicketServiceDates,
        };

        var f = ServiceRow.All();

        var today = SampleData.Today;
        var n7 = f.Count(x => x.S.Start >= today && x.S.Start <= today.AddDays(7));
        var variation = f.Sum(x => x.Variation);
        var common = new[]
        {
            new Kpi("Total Duration", f.Sum(x => x.S.Days) + "d"),
            new Kpi("Total Selling", Fmt.Inr(f.Sum(x => x.S.Sell)), "var(--color-action-blue)"),
            new Kpi("Total Negotiated", Fmt.Inr(f.Sum(x => x.Negotiated)), "var(--tint-green-ink)"),
            new Kpi("Variation", Fmt.Signed(variation), variation >= 0 ? "var(--ok-ink)" : "var(--color-danger)"),
        };
        page.ResultLabel = f.Count + " services";

        if (page.IsTrialBalance)
        {
            var tb = page.Tb = TrialBalance.Build();
            page.ResultLabel = "";
            if (tb.VendorId is not null)
            {
                page.Kpis.Add(new Kpi("Vendor", SampleData.VendorName(tb.VendorId), Sub: tb.VendorId));
                page.Kpis.Add(new Kpi("Period", tb.Period, Sub: tb.Fy));
                page.Kpis.Add(new Kpi("Currency", tb.Currency, Big: true));
                // Closing balance and the ledger read from the same running totals.
                page.Kpis.Add(new Kpi("Closing balance", tb.ClosingAmountText, tb.ClosingColor, true, tb.ClosingNote));
                page.Kpis.Add(new Kpi("Dr = Cr check", tb.Balanced ? "✓ Balanced" : "✗ Mismatch", tb.Balanced ? "var(--ok-ink)" : "var(--color-danger)", true,
                    "Dr " + tb.Money(tb.DrSide) + " · Cr " + tb.Money(tb.CrSide)));
                page.ResultLabel = tb.EntryCount + " entries";
            }
        }
        else if (page.IsVendorView)
        {
            page.Kpis.Add(new Kpi("Next 7 Days", n7.ToString(), n7 > 0 ? "var(--alert-ink)" : "var(--color-navy)", true));
            page.Kpis.Add(new Kpi("Total Services", f.Count.ToString(), Big: true));
            page.Kpis.Add(new Kpi("Total Vendors", f.Select(x => x.S.VendorId).Distinct().Count().ToString(), Big: true));
            page.Kpis.AddRange(common);
            var sorted = f.OrderBy(x => x.S.Start).ToList();
            (page.Rows, page.Pager) = Pager.Paginate(sorted, q.Page);
        }
        else
        {
            var groups = f.GroupBy(x => x.S.City).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new ServiceGroup(g.Key, g.ToList())).ToList();
            (page.Groups, page.Pager) = Pager.Paginate(groups, q.Page);
            page.Kpis.Add(new Kpi("Cities", groups.Count.ToString(), Big: true));
            page.Kpis.Add(new Kpi("Total Services", f.Count.ToString(), Big: true));
            page.Kpis.Add(new Kpi("Total Pax", ServiceRow.PaxOf(f).ToString(), Big: true));
            page.Kpis.AddRange(common);
        }
        return page;
    }
}
