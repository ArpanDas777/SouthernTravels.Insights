using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

public sealed record DayInfo(int D, string Dow, bool Weekend, bool InRange);

public readonly record struct Split(int N, int P, int C)
{
    public int Total => N + P + C;
    public static Split operator +(Split a, Split b) => new(a.N + b.N, a.P + b.P, a.C + b.C);
}

public sealed record WeekInfo(int Wi, List<DayInfo> Days)
{
    public string Range => Days[0].D + "–" + Days[^1].D;
}

/// <summary>A calendar column: either a single day or a collapsed week.</summary>
public sealed record CalUnit(DayInfo? Day, WeekInfo? Week)
{
    public Split Get(Func<int, Split> get) =>
        Day is not null ? get(Day.D) : Week!.Days.Aggregate(new Split(), (acc, d) => acc + get(d.D));
}

public sealed class WorkUser
{
    public required string Name { get; init; }
    public required Dictionary<int, Split> Daily { get; init; }
    public Split InRange { get; init; }
    public double Pct => WorkloadMath.Pct(InRange.C, InRange.Total);
}

public sealed class WorkBranch
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required List<WorkUser> Users { get; init; }
    public Split Totals => Users.Aggregate(new Split(), (a, u) => a + u.InRange);
    public double Pct => WorkloadMath.Pct(Totals.C, Totals.Total);
    public Split Day(int d) => Users.Aggregate(new Split(), (a, u) => a + u.Daily[d]);
}

public static class WorkloadMath
{
    public static double Pct(int c, int t) => t > 0 ? c / (double)t * 100 : 0;

    /// <summary>% Complete pill tone: ≥45 teal, ≥43 blue, otherwise slate.</summary>
    public static string PctTone(double p) => p >= 45 ? "pp-hi" : p >= 43 ? "pp-mid" : "pp-lo";
}

public sealed class OpsUserWorkloadPage : ReportPage
{
    public List<WorkBranch> Branches { get; set; } = new();
    public Split GrandTotal { get; set; }
    public List<WeekInfo> Weeks { get; set; } = new();
    public List<CalUnit> Units { get; set; } = new();
    /// <summary>Services behind each clickable count, keyed "user|status".</summary>
    public Dictionary<string, List<ServiceRow>> Drill { get; set; } = new();
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
            Title = "OPS User Workload",
            Description = "OPS User Workload — a day-by-day breakdown of service counts and statuses assigned to each OPS user and branch. Tracks total services per user alongside a status-wise split, helping supervisors monitor daily throughput, flag overloaded users, and maintain workload balance across branches.",
            Query = q,
            HasToolbar = false,
            Tabs = Tabs,
            ActiveTab = ReportPage.ResolveTab(q, Tabs),
        };

        var wf = Fmt.ParseIso(q.WFrom) ?? DateTime.MinValue;
        var wt = Fmt.ParseIso(q.WTo) ?? DateTime.MaxValue;
        string[] dows = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
        var dl = Enumerable.Range(1, 30).Select(d =>
        {
            var dt = new DateTime(2026, 9, d);
            var w = (int)dt.DayOfWeek;
            return new DayInfo(d, dows[w], w == 0 || w == 6, dt >= wf && dt <= wt);
        }).ToList();

        page.Branches = Sample.Select((b, bi) => new WorkBranch
        {
            Name = b.Branch,
            Code = b.Code,
            Users = b.Users.Select((u, i) =>
            {
                var daily = Distribute(dl, u.Total, u.New, u.Done, i * 5 + bi * 3);
                var inR = dl.Where(x => x.InRange).Aggregate(new Split(), (a, x) => a + daily[x.D]);
                return new WorkUser { Name = u.Name, Daily = daily, InRange = inR };
            }).ToList(),
        }).ToList();
        page.GrandTotal = page.Branches.Aggregate(new Split(), (a, b) => a + b.Totals);

        // Calendar weeks (prototype groups by floor(day / 7)) and visible columns.
        page.Weeks = dl.Where(x => x.InRange).GroupBy(x => x.D / 7).Select(g => new WeekInfo(g.Key, g.ToList())).ToList();
        foreach (var wk in page.Weeks)
        {
            if (q.CollapsedWeeks.Contains(wk.Wi)) page.Units.Add(new CalUnit(null, wk));
            else page.Units.AddRange(wk.Days.Select(d => new CalUnit(d, null)));
        }

        // Count drill-downs: services are assigned round-robin to each branch's OPS users.
        int di = 0, hi = 0;
        string[] del = { "Versha", "Ujjwal", "Gaurav" }, hyd = { "Chandu", "Jhansi", "Gopi" };
        foreach (var x in ServiceRow.All())
        {
            var user = x.S.OpsBranch == "DEL001" ? del[di++ % 3] : hyd[hi++ % 3];
            var key = user + "|" + x.S.OpsDisplay;
            if (!page.Drill.TryGetValue(key, out var list)) page.Drill[key] = list = new List<ServiceRow>();
            list.Add(x);
        }
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
