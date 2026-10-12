using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

public sealed record Kpi(string Label, string Value, string Color = "var(--color-navy)", bool Big = false, string? Sub = null, string? Tip = null);

public sealed record TabDef(string Key, string Label);

/// <summary>A dropdown shown in the filter panel (visual only).</summary>
public sealed record FilterDef(string Label, IReadOnlyList<string> Options);

public sealed record NavItem(string Id, string Action, string Label, string Icon);

/// <summary>One pagination control; Href is a plain link to the page (null when disabled).</summary>
public sealed record PageBtn(string Label, string? Href, bool Disabled, bool Active);

public sealed class Pager
{
    public const int Size = 10;
    public int Page { get; init; }
    public int Total { get; init; }
    /// <summary>Tab and Show-all state carried by the paging links (set by ReportPage.Pager).</summary>
    public string Tab { get; set; } = "";
    public bool ShowAll { get; set; }

    public string Href(int page) => "?" + (Tab.Length > 0 ? "tab=" + Tab + "&" : "") + "page=" + page + (ShowAll ? "&all=1" : "");

    public string Label => Total > 0
        ? $"Showing {(Page - 1) * Size + 1}–{Math.Min(Page * Size, Total)} of {Total}"
        : "No results";

    public IEnumerable<PageBtn> Buttons
    {
        get
        {
            var pages = Math.Max(1, (int)Math.Ceiling(Total / (double)Size));
            var p = Page;
            var nums = new List<int?>();
            void Rg(int a, int b) { for (var i = a; i <= b; i++) nums.Add(i); }
            if (pages <= 7) Rg(1, pages);
            else if (p <= 4) { Rg(1, 5); nums.Add(null); nums.Add(pages); }
            else if (p >= pages - 3) { nums.Add(1); nums.Add(null); Rg(pages - 4, pages); }
            else { nums.Add(1); nums.Add(null); nums.Add(p - 1); nums.Add(p); nums.Add(p + 1); nums.Add(null); nums.Add(pages); }

            yield return new PageBtn("‹", p <= 1 ? null : Href(p - 1), p <= 1, false);
            foreach (var n in nums) yield return n is null ? new PageBtn("…", null, true, false) : new PageBtn(n.ToString()!, Href(n.Value), false, n == p);
            yield return new PageBtn("›", p >= pages ? null : Href(p + 1), p >= pages, false);
        }
    }

    public static (List<T> Slice, Pager Pager) Paginate<T>(IReadOnlyList<T> rows, int requested)
    {
        var page = Math.Min(requested, Math.Max(1, (int)Math.Ceiling(rows.Count / (double)Size)));
        return (rows.Skip((page - 1) * Size).Take(Size).ToList(), new Pager { Page = page, Total = rows.Count });
    }
}

/// <summary>Everything the shared report chrome (period bar, title, KPIs, tabs, toolbar, filters, pager) needs.</summary>
public abstract class ReportPage
{
    public static readonly IReadOnlyList<NavItem> Nav = new NavItem[]
    {
        new("r1", "TicketServiceStatus", "Ticket & Service Status", "M3 9a2 2 0 0 0 0 6v3a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1v-3a2 2 0 0 1 0-6V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1zM13 5v2M13 11v2M13 17v2"),
        new("r2", "OpsUserWorkload", "OPS User Workload", "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"),
        new("r3", "ServiceWise", "Service wise Report", "M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"),
        new("r4", "VendorPayment", "Vendor Payment details", "M4 5h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2zM2 10h20M6 15h4"),
        new("r5", "Cancellation", "Cancellation Report", "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20zM15 9l-6 6M9 9l6 6"),
        new("r6", "Advance", "Advance Report", "M2 7h20v10H2zM12 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM6 12h.01M18 12h.01"),
        new("r7", "WeeklySalesTrend", "Weekly Sales trend", "M3 17l6-6 4 4 8-8M15 7h6v6"),
        new("r8", "DailyEmpSales", "Daily EMP Sales", "M4 20V10M10 20V4M16 20v-8M22 20H2"),
        new("r9", "SalesVsTarget", "Sales vs Target", "M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20zM12 18a6 6 0 1 0 0-12 6 6 0 0 0 0 12zM12 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4z"),
    };

    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ReportQuery Query { get; init; }

    /// <summary>Shared Period selector options (visual only).</summary>
    public static readonly string[] PeriodOptions = { "All time", "Today", "This Week", "This Month", "This Quarter", "This Year", "Custom" };

    public string SearchPlaceholder { get; init; } = "";
    /// <summary>Date field the shared Period control filters by; empty when the report has no Period control.</summary>
    public string PeriodField { get; init; } = "";
    public bool ShowPeriod => PeriodField.Length > 0;
    public bool HasToolbar { get; init; } = true;
    public bool HasExport { get; init; } = true;
    /// <summary>Expand All / Collapse All buttons (hidden on the Trial Balance ledger, which has no drill-down).</summary>
    public bool HasExpand { get; init; } = true;
    /// <summary>Minimum KPI card width for the auto-fit grid.</summary>
    public int KpiMin { get; set; }
    /// <summary>Auto-fit KPI grid column width: the report's own minimum, else tighter for 9+ cards.</summary>
    public int KpiMinWidth => KpiMin > 0 ? KpiMin : Kpis.Count > 8 ? 104 : 120;
    /// <summary>Uniform KPI value size (Weekly Sales trend uses 13px for every card); 0 = 16px counts / 12px money.</summary>
    public int KpiValueSize { get; init; }
    /// <summary>Optional partial rendered at the right of the title row (date pickers, Trial Balance controls).</summary>
    public string? TitleControls { get; init; }
    /// <summary>Optional partial rendered inside the toolbar (Comparison view switch and dropdowns).</summary>
    public string? ToolbarExtra { get; init; }
    /// <summary>Daily EMP Sales uses a slightly different tab strip placed above the toolbar.</summary>
    public bool CompactTabs { get; init; }

    public IReadOnlyList<TabDef> Tabs { get; init; } = Array.Empty<TabDef>();
    public string ActiveTab { get; init; } = "";

    public List<Kpi> Kpis { get; } = new();
    public string ResultLabel { get; set; } = "";

    public IReadOnlyList<FilterDef> Filters { get; init; } = Array.Empty<FilterDef>();
    public IReadOnlyList<string> DateFields { get; init; } = Array.Empty<string>();

    private Pager? _pager;
    public Pager? Pager
    {
        get => _pager;
        set { _pager = value; if (value is not null) { value.Tab = ActiveTab; value.ShowAll = Query.ShowAll; } }
    }

    /// <summary>Plain link to a tab (keeps the Show-all state).</summary>
    public string TabHref(string key) => "?tab=" + key + (Query.ShowAll ? "&all=1" : "");

    public static string ResolveTab(ReportQuery q, IReadOnlyList<TabDef> tabs) =>
        tabs.Any(t => t.Key == q.Tab) ? q.Tab! : tabs.Count > 0 ? tabs[0].Key : "";
}
