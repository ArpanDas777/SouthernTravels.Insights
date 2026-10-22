using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Reports;

public sealed record TicketRow(Ticket T, bool Mine)
{
    /// <summary>OPS branches involved, e.g. "DEL001 / HYD001".</summary>
    public string OpsBranches => string.Join(" / ", T.Services.Select(s => s.OpsBranch).Distinct());
    public bool HasBalance => T.Balance > 0;
    public string BalancePct => Fmt.Pct0(T.Balance / T.Grand * 100);
    public string PaidPct => Fmt.Pct0(T.Paid / T.Grand * 100);

    public int DaysLeft => (int)Fmt.Round((T.Start - SampleData.Today).TotalDays);

    public string DaysLeftText => DaysLeft < 0 ? "Departed" : DaysLeft == 0 ? "Departs today" : DaysLeft + (DaysLeft == 1 ? " day left" : " days left");

    /// <summary>"soon" (0–7 days), "past" or "later".</summary>
    public string DaysLeftTone => DaysLeft >= 0 && DaysLeft <= 7 ? "soon" : DaysLeft < 0 ? "past" : "later";
}

public sealed record VendorItem(Ticket T, Service S)
{
    public double Profit => S.Sell - S.Payable;
    /// <summary>Profit as a percentage of the selling rate ("12.5%").</summary>
    public string ProfitPct => Fmt.Fixed1(Profit / S.Sell * 100) + "%";
}

public sealed record VendorGroup(string Id, string Name, List<VendorItem> Items)
{
    public string Cities => string.Join(", ", Items.Select(x => x.S.City).Distinct());
    public IEnumerable<string> Types => Items.Select(x => x.S.Type).Distinct();
    public int TicketCount => Items.Select(x => x.T.No).Distinct().Count();
    public double Sell => Items.Sum(x => x.S.Sell);
    public double Payable => Items.Sum(x => x.S.Payable);
    public double Balance => Items.Sum(x => x.S.VendorBalance);
    public bool Active => Items.Any(x => x.S.Status != "Cancelled");
    public bool HasBalance => Balance > 0;
}

public sealed class TicketServiceStatusPage : ReportPage
{
    public bool IsTicketView => ActiveTab == "ticket";
    /// <summary>Plain link that toggles the "mine only" / "all files" list.</summary>
    public string ShowAllHref => Query.ShowAll ? "?tab=" + ActiveTab : "?tab=" + ActiveTab + "&all=1";
    public string ShowAllLabel => Query.ShowAll ? "Show mine only" : "Show all";
    public List<TicketRow> TicketRows { get; set; } = new();
    public List<VendorGroup> VendorRows { get; set; } = new();
}

public static class TicketServiceStatusReport
{
    private static readonly TabDef[] Tabs = { new("ticket", "Ticket-Wise View"), new("vendor", "Vendor-Wise View") };

    /// <param name="filters">The filter dropdowns (from the Operations API); null uses the built-in lists.</param>
    public static TicketServiceStatusPage Build(ReportQuery q, IReadOnlyList<FilterDef>? filters = null)
    {
        var tab = ReportPage.ResolveTab(q, Tabs);
        var page = new TicketServiceStatusPage
        {
            Id = "r1",
            StickyHeader = true,
            Title = "Ticket & Service Status",
            Description = "Ticket & Service Status — an employee-wise view of every ticket and its linked services. Covers OPS Activity (what's New, In Progress, or Completed per OPS user), Pending Follow-up Identification (tickets needing immediate attention), Service Completion Tracking (a closed-loop log of everything marked Completed), Branch-wise OPS Performance (activity grouped by OPS Branch), and Overall OPS Status Visibility across the board.",
            Query = q,
            SearchPlaceholder = "Search MBK No., Ticket No., Passenger, File No.",
            PeriodField = "Booking Date",
            Tabs = Tabs,
            ActiveTab = tab,
            Filters = filters ?? CommonFilters.TicketService,
            DateFields = CommonFilters.TicketServiceDates,
        };

        var me = SampleData.Me;
        var tk = SampleData.Tickets
            .Where(t => q.ShowAll || t.IsAssignedTo(me))
            .OrderBy(t => t.IsAssignedTo(me) ? 0 : 1).ThenBy(t => t.Start)
            .ToList();

        // KPIs (recomputed from the filtered ticket set).
        var tats = tk.SelectMany(t => t.Services).Where(s => s.OpsStatus == "Completed" && s.Status == "Confirmed" && s.Tat is not null).Select(s => s.Tat!.Value).ToList();
        page.Kpis.Add(new Kpi("Total Tickets", tk.Count.ToString(), "var(--color-navy)", true));
        page.Kpis.Add(new Kpi("New", tk.Count(t => t.OverallOps == "New").ToString(), "var(--ink-muted)", true));
        page.Kpis.Add(new Kpi("In Progress", tk.Count(t => t.OverallOps == "In Progress").ToString(), "var(--info-ink)", true));
        page.Kpis.Add(new Kpi("Completed", tk.Count(t => t.OverallOps == "Completed").ToString(), "var(--tint-green-ink)", true));
        page.Kpis.Add(new Kpi("New → Completed TAT", tats.Count > 0 ? Fmt.Fixed1(tats.Average()) + " days" : "-", "var(--color-action-blue)", false, null,
            "Average days from service creation to confirmation, across Completed + Confirmed services"));

        if (page.IsTicketView)
        {
            var rows = tk.Select(t => new TicketRow(t, t.IsAssignedTo(me))).ToList();
            (page.TicketRows, page.Pager) = Pager.Paginate(rows, q.Page);
            page.ResultLabel = tk.Count + " tickets";
        }
        else
        {
            var groups = tk.SelectMany(t => t.Services.Select(s => new VendorItem(t, s)))
                .GroupBy(x => x.S.VendorId)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new VendorGroup(g.Key, SampleData.VendorName(g.Key), g.ToList()))
                .ToList();
            (page.VendorRows, page.Pager) = Pager.Paginate(groups, q.Page);
            page.ResultLabel = groups.Count + " vendors";
        }
        return page;
    }
}
