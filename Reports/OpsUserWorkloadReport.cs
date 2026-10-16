using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

public sealed record DayInfo(int D, string Dow, bool Weekend);

public readonly record struct Split(int N, int P, int C)
{
    public int Total => N + P + C;
    public static Split operator +(Split a, Split b) => new(a.N + b.N, a.P + b.P, a.C + b.C);
}

public sealed record WeekInfo(int Wi, List<DayInfo> Days)
{
    public string Range => Days[0].D + "–" + Days[^1].D;
    public string Label => "Week " + (Wi + 1) + " (" + Range + ")";
}

/// <summary>One clickable count in the summary table (New / In Progress / Completed) and the services behind it.</summary>
public sealed record StatusCount(string Status, string Cls, int Count, List<ServiceRow> Services);

/// <summary>A pre-computed % Complete figure with its pill tone.</summary>
public sealed record PctCell(string Text, string Tone)
{
    public static PctCell Of(int done, int total)
    {
        var p = total > 0 ? done / (double)total * 100 : 0;
        return new PctCell(Fmt.Fixed1(p) + "%", p >= 45 ? "pp-hi" : p >= 43 ? "pp-mid" : "pp-lo");
    }
}

public sealed class WorkUser
{
    public required string Name { get; init; }
    public Split Totals { get; init; }
    public required PctCell Pct { get; init; }
    public required List<StatusCount> Counts { get; init; }
    /// <summary>Calendar cell per day, in column order.</summary>
    public required List<Split> Cells { get; init; }
}

public sealed class WorkBranch
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required List<WorkUser> Users { get; init; }
    public Split Totals { get; init; }
    public required PctCell Pct { get; init; }
    public required List<Split> Cells { get; init; }
}

public sealed class OpsUserWorkloadPage : ReportPage
{
    public const string RangeFrom = "2026-09-01", RangeTo = "2026-09-30";

    public List<WorkBranch> Branches { get; set; } = new();
    public Split GrandTotal { get; set; }
    public PctCell GrandPct { get; set; } = new("", "");
    public List<WeekInfo> Weeks { get; set; } = new();
    public List<DayInfo> Days { get; set; } = new();
    public List<Split> GrandCells { get; set; } = new();
    /// <summary>Calendar column count: user, status, one per day, total.</summary>
    public int CalColumns { get; set; }
    public bool IsCalendar => ActiveTab == "calendar";
}

public static class OpsUserWorkloadReport
{
    private static readonly TabDef[] Tabs = { new("summary", "Summary"), new("calendar", "Daily Calendar View") };

    // [name, total, new, completed] — sample monthly figures per OPS user.
    private static readonly (string Branch, string Code, (string Name, int Total, int New, int Done)[] Users)[] Sample =
    {
        ("Delhi", "DEL001", new[] { ("Versha", 229, 47, 97), ("Ujjwal", 217, 38, 104), ("Gaurav", 231, 53, 96) }),
        ("Hyderabad", "HYD001", new[] { ("Chandu", 240, 46, 100), ("Jhansi", 236, 65, 98), ("Gopi", 208, 36, 96) }),
    };

    public static OpsUserWorkloadPage Build(ReportQuery q)
    {
        var page = new OpsUserWorkloadPage
        {
            Id = "r2",
            TitleControls = "_OpsWorkloadPeriod",
            Title = "OPS User Workload",
            Description = "OPS User Workload — a day-by-day breakdown of service counts and statuses assigned to each OPS user and branch. Tracks total services per user alongside a status-wise split, helping supervisors monitor daily throughput, flag overloaded users, and maintain workload balance across branches.",
            Query = q,
            HasToolbar = false,
            Tabs = Tabs,
            ActiveTab = ReportPage.ResolveTab(q, Tabs),
        };

        // September 2026, every day shown.
        string[] dows = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
        var dl = Enumerable.Range(1, 30).Select(d =>
        {
            var w = (int)new DateTime(2026, 9, d).DayOfWeek;
            return new DayInfo(d, dows[w], w == 0 || w == 6);
        }).ToList();
        page.Days = dl;
        page.CalColumns = dl.Count + 3;
        page.Weeks = dl.GroupBy(x => x.D / 7).Select(g => new WeekInfo(g.Key, g.ToList())).ToList();

        // Services behind each clickable count: assigned round-robin to each branch's OPS users.
        var drill = new Dictionary<string, List<ServiceRow>>();
        int di = 0, hi = 0;
        string[] del = { "Versha", "Ujjwal", "Gaurav" }, hyd = { "Chandu", "Jhansi", "Gopi" };
        foreach (var x in ServiceRow.All())
        {
            var user = x.S.OpsBranch == "DEL001" ? del[di++ % 3] : hyd[hi++ % 3];
            var key = user + "|" + x.S.OpsDisplay;
            if (!drill.TryGetValue(key, out var list)) drill[key] = list = new List<ServiceRow>();
            list.Add(x);
        }
        List<ServiceRow> Services(string user, string status) => drill.GetValueOrDefault(user + "|" + status) ?? new List<ServiceRow>();

        var branches = new List<(string Name, string Code, List<(string Name, Dictionary<int, Split> Daily, Split Totals)> Users)>();
        foreach (var (b, bi) in Sample.Select((b, bi) => (b, bi)))
        {
            var users = b.Users.Select((u, i) =>
            {
                var daily = Distribute(dl, u.Total, u.New, u.Done, i * 5 + bi * 3);
                return (u.Name, daily, Totals: dl.Aggregate(new Split(), (a, x) => a + daily[x.D]));
            }).ToList();
            branches.Add((b.Branch, b.Code, users));
        }

        Split Sum(IEnumerable<Split> s) => s.Aggregate(new Split(), (a, x) => a + x);
        page.Branches = branches.Select(b =>
        {
            var totals = Sum(b.Users.Select(u => u.Totals));
            return new WorkBranch
            {
                Name = b.Name,
                Code = b.Code,
                Totals = totals,
                Pct = PctCell.Of(totals.C, totals.Total),
                Cells = dl.Select(x => Sum(b.Users.Select(u => u.Daily[x.D]))).ToList(),
                Users = b.Users.Select(u => new WorkUser
                {
                    Name = u.Name,
                    Totals = u.Totals,
                    Pct = PctCell.Of(u.Totals.C, u.Totals.Total),
                    Cells = dl.Select(x => u.Daily[x.D]).ToList(),
                    Counts = new List<StatusCount>
                    {
                        new("New", "n", u.Totals.N, Services(u.Name, "New")),
                        new("In Progress", "p", u.Totals.P, Services(u.Name, "In Progress")),
                        new("Completed", "c", u.Totals.C, Services(u.Name, "Completed")),
                    },
                }).ToList(),
            };
        }).ToList();
        page.GrandTotal = Sum(page.Branches.Select(b => b.Totals));
        page.GrandPct = PctCell.Of(page.GrandTotal.C, page.GrandTotal.Total);
        page.GrandCells = dl.Select((_, i) => Sum(page.Branches.Select(b => b.Cells[i]))).ToList();
        return page;
    }

    /// <summary>Spreads a monthly total over the days (weekdays weigh 2, weekends 1), then splits each day New / In Progress / Completed.</summary>
    private static Dictionary<int, Split> Distribute(List<DayInfo> dl, int total, int nNew, int nDone, int offset)
    {
        var units = dl.Sum(x => x.Weekend ? 1 : 2);
        var res = new Dictionary<int, int>();
        var used = 0;
        foreach (var x in dl)
        {
            var n = total * (x.Weekend ? 1 : 2) / units;
            res[x.D] = n;
            used += n;
        }
        for (var i = 0; used < total; i++, used++) res[dl[(i + offset) % dl.Count].D]++;

        return dl.ToDictionary(x => x.D, x =>
        {
            var n = res[x.D];
            var a = (int)Fmt.Round(n * nNew / (double)total);
            var c = (int)Fmt.Round(n * nDone / (double)total);
            return new Split(a, Math.Max(0, n - a - c), c);
        });
    }
}
