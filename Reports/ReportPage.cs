using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

public sealed record Kpi(string Label, string Value, string Color = "var(--color-navy)", bool Big = false, string? Sub = null, string? Tip = null);

public sealed record TabDef(string Key, string Label);

public sealed record FilterDef(string Label, IReadOnlyList<string> Options)
{
    public string Slug => ReportQuery.Slug(Label);
}

public sealed record NavItem(string Id, string Action, string Label, string Icon);

public sealed record PageBtn(string Label, int? Page, bool Disabled, bool Active);

public sealed class Pager
{
    public const int Size = 10;
    public int Page { get; init; }
    public int Total { get; init; }

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

            yield return new PageBtn("‹", p - 1, p <= 1, false);
            foreach (var n in nums) yield return n is null ? new PageBtn("…", null, true, false) : new PageBtn(n.ToString()!, n, false, n == p);
            yield return new PageBtn("›", p + 1, p >= pages, false);
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
    };

    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ReportQuery Query { get; init; }

    public string SearchPlaceholder { get; init; } = "";
    /// <summary>Date field the shared Period control filters by; empty when the report has no Period control.</summary>
    public string PeriodField { get; init; } = "";
    public bool ShowPeriod => PeriodField.Length > 0;
    public bool HasToolbar { get; init; } = true;

    public IReadOnlyList<TabDef> Tabs { get; init; } = Array.Empty<TabDef>();
    public string ActiveTab { get; init; } = "";

    public List<Kpi> Kpis { get; } = new();
    public string ResultLabel { get; set; } = "";

    public IReadOnlyList<FilterDef> Filters { get; init; } = Array.Empty<FilterDef>();
    public IReadOnlyList<string> DateFields { get; init; } = Array.Empty<string>();

    public Pager? Pager { get; set; }

    public static string ResolveTab(ReportQuery q, IReadOnlyList<TabDef> tabs) =>
        tabs.Any(t => t.Key == q.Tab) ? q.Tab! : tabs.Count > 0 ? tabs[0].Key : "";
}
