using System.Globalization;
using System.Text.RegularExpressions;
using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

public sealed record SalesDay(DateTime D, double Gross, double Canx, double Net, bool Future, double PrevNet, double PrevGross)
{
    public bool Weekend => SalesSample.IsWeekend(D);
    /// <summary>Heatmap intensity (0–.56) for the highlighted metric.</summary>
    public double Heat { get; init; }

    /// <summary>Gross change vs the same weekday last week ("▲ +4.2%"); empty without data.</summary>
    public string TrendText => Future || PrevGross == 0 ? "" : (Gross >= PrevGross ? "▲ +" : "▼ −") + Fmt.Fixed1(Math.Abs((Gross - PrevGross) / PrevGross * 100)) + "%";
    public bool TrendUp => Gross >= PrevGross;

    /// <summary>Hover text for the heatmap cell.</summary>
    public string Tip => WeeklySalesTrendPage.DayNames[(int)D.DayOfWeek] + " " + WeeklySalesTrendPage.Dmy(D) + " · " + (Future ? "No data yet" : WeeklySalesTrendPage.Wow(Net, PrevNet) + " vs same day last week");
}
/// <summary>One day in the Table view; amounts and the week-on-week change are display-ready.</summary>
public sealed record WeekDayRow(string Date, string Day, bool Weekend, bool Future, string Gross, string Canx, string Net, string Wow, bool WowUp);
/// <summary>One Monday–Sunday week of the Table view, with its totals and days (the current week is open).</summary>
public sealed record WeekRow(string Label, string Gross, string Canx, string Net, string Wow, bool WowUp, bool Open, List<WeekDayRow> Days);
public sealed record ChartTick(string Bottom, string Label);
public sealed record ChartSeries(string Name, string Color, string Points);
public sealed record ChartCol(bool Weekend, string Label, string Tip);

public sealed class WeeklySalesTrendPage : ReportPage
{
    public static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
    private static readonly string[] Mon = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public string? Branch { get; init; }
    public string? Type { get; init; }
    /// <summary>Highlighted metric: net / gross / canx.</summary>
    public string Metric { get; init; } = "net";
    /// <summary>Selected view: calendar, line or table (the "tab" link value).</summary>
    public string View { get; init; } = "calendar";
    public static readonly (string Key, string Label)[] Views = { ("calendar", "Calendar"), ("line", "Line"), ("table", "Table") };
    public List<WeekRow> WeekRows { get; set; } = new();
    public List<SalesDay> Days { get; set; } = new();
    /// <summary>Monday–Sunday weeks of the selected range.</summary>
    public List<List<SalesDay?>> Weeks { get; set; } = new();
    public List<ChartTick> Ticks { get; set; } = new();
    public List<ChartSeries> Series { get; set; } = new();
    public List<ChartCol> Cols { get; set; } = new();

    public string MetricLabel => Metric switch { "gross" => "Gross sales", "canx" => "Cancellation", _ => "Net sales" };
    public string HeatColor => Metric switch { "gross" => "var(--ok-solid)", "canx" => "var(--danger-solid)", _ => "var(--chart-1)" };

    public static string Dmy(DateTime d) => d.ToString("dd/MM/yy", CultureInfo.InvariantCulture);
    public static string CellDate(DateTime d) => d.Day.ToString("D2") + "/" + Mon[d.Month - 1] + "/" + d.Year;

    /// <summary>Net sales week-on-week ("▲ +3.1%", "–" with no prior data).</summary>
    public static string Wow(double net, double prevNet)
    {
        if (prevNet == 0) return "–";
        var v = (net - prevNet) / prevNet * 100;
        return v >= 0 ? "▲ +" + Fmt.Fixed1(v) + "%" : "▼ −" + Fmt.Fixed1(Math.Abs(v)) + "%";
    }
}

public static class WeeklySalesTrendReport
{
    public static WeeklySalesTrendPage Build(ReportQuery q)
    {
        var page = new WeeklySalesTrendPage
        {
            Id = "r7",
            Title = "Weekly Sales trend",
            Description = "Weekly Sales trend — day-by-day gross sales, cancellations and net sales, grouped into Monday–Sunday weeks. WoW % compares net sales with the same weekday in the previous week. The current week opens by default; weekends are shaded.",
            Query = q,
            HasToolbar = false,
            KpiMin = 98,
            KpiValueSize = 13,
            From = "2026-10-01",
            To = "2026-10-31",
            View = new[] { "calendar", "line", "table" }.Contains(q.Tab) ? q.Tab! : "calendar",
        };

        var raw = new List<SalesDay>();
        for (var d = new DateTime(2026, 10, 1); d <= new DateTime(2026, 10, 31); d = d.AddDays(1))
        {
            var g = SalesSample.Trend(d, null, null);
            var p = SalesSample.Trend(d.AddDays(-7), null, null);
            raw.Add(d > SalesSample.DataTill ? new SalesDay(d, 0, 0, 0, true, p.Net, p.Gross) : new SalesDay(d, g.Gross, g.Canx, g.Net, false, p.Net, p.Gross));
        }

        // Heatmap intensity over days that have data (net sales is the highlighted metric).
        var live = raw.Where(x => !x.Future).ToList();
        double lo = live.Count > 0 ? live.Min(x => x.Net) : 0, hi = live.Count > 0 ? live.Max(x => x.Net) : 0;
        var days = raw.Select(x => x.Future ? x : x with { Heat = .06 + (hi > lo ? (x.Net - lo) / (hi - lo) : 0) * .5 }).ToList();
        page.Days = days;
        // Monday–Sunday weeks, seven slots each (null where the week starts / ends mid-week).
        page.Weeks = days.GroupBy(x => x.D.AddDays(-(((int)x.D.DayOfWeek + 6) % 7)))
            .Select(w => Enumerable.Range(0, 7).Select(k => w.FirstOrDefault(y => ((int)y.D.DayOfWeek + 6) % 7 == k)).ToList()).ToList();
        // Table view: Monday–Sunday weeks with totals; the week containing the sample "today" opens by default.
        var today = new DateTime(2026, 10, 5);
        foreach (var w in days.GroupBy(x => x.D.AddDays(-(((int)x.D.DayOfWeek + 6) % 7))).OrderBy(g => g.Key))
        {
            var done = w.Where(x => !x.Future).ToList();
            double wg = done.Sum(x => x.Gross), wc = done.Sum(x => x.Canx), wn = done.Sum(x => x.Net), wp = done.Sum(x => x.PrevNet);
            var first = w.Min(x => x.D);
            var last = w.Max(x => x.D);
            page.WeekRows.Add(new WeekRow(
                Fmt.Date(first) + " – " + Fmt.Date(last),
                done.Count > 0 ? Fmt.Inr(wg) : "-", done.Count > 0 ? "−" + Fmt.Inr(wc) : "-", done.Count > 0 ? Fmt.Inr(wn) : "-",
                done.Count > 0 ? WeeklySalesTrendPage.Wow(wn, wp) : "–", wn >= wp, today >= w.Key && today <= w.Key.AddDays(6),
                w.OrderBy(x => x.D).Select(x => new WeekDayRow(Fmt.Date(x.D), WeeklySalesTrendPage.DayNames[(int)x.D.DayOfWeek], x.Weekend, x.Future,
                    x.Future ? "-" : Fmt.Inr(x.Gross), x.Future ? "-" : "−" + Fmt.Inr(x.Canx), x.Future ? "-" : Fmt.Inr(x.Net),
                    x.Future ? "–" : WeeklySalesTrendPage.Wow(x.Net, x.PrevNet), x.Net >= x.PrevNet)).ToList()));
        }
        // KPIs
        double tg = days.Sum(x => x.Gross), tc = days.Sum(x => x.Canx), tn = days.Sum(x => x.Net);
        var best = days.OrderByDescending(x => x.Net).FirstOrDefault();
        page.Kpis.AddRange(new[]
        {
            new Kpi("Gross sales", Fmt.Inr(tg), Big: true),
            new Kpi("Cancellation", days.Count > 0 ? "−" + Fmt.Inr(tc) : "–", "var(--color-danger)", true),
            new Kpi("Net sales", Fmt.Inr(tn), "var(--color-action-blue)", true),
            new Kpi("Cancellation % of gross", tg != 0 ? Fmt.Fixed1(tc / tg * 100) + "%" : "-", "var(--warn-ink-deep)", true),
            new Kpi("Best day · " + (best is null ? "-" : WeeklySalesTrendPage.Dmy(best.D)), best is null ? "-" : Fmt.Inr(best.Net), "var(--ok-ink)", true),
            new Kpi("Avg daily net", live.Count > 0 ? Fmt.Inr(tn / live.Count) : "-", Big: true),
        });

        // Line chart: gross, cancellation and net per day.
        var n = Math.Max(1, days.Count);
        var top = Math.Max(1, Math.Ceiling(Math.Max(1, days.Count > 0 ? days.Max(x => x.Gross) : 0) / 2e5) * 2e5);
        page.Ticks = Enumerable.Range(0, 5).Select(i => new ChartTick(i * 25 + "%", ChartLabel(top * i / 4))).ToList();
        string Pts(Func<SalesDay, double> v) => string.Join(" ", days.Select((x, i) => (x, i)).Where(p => !p.x.Future)
            .Select(p => ((p.i + .5) / n * 1000).ToString("0.0", CultureInfo.InvariantCulture) + "," + (190 - v(p.x) / top * 190).ToString("0.0", CultureInfo.InvariantCulture)));
        page.Series = new List<ChartSeries>
        {
            new("Gross sales", "var(--ok-solid)", Pts(x => x.Gross)),
            new("Cancellation", "var(--danger-solid)", Pts(x => x.Canx)),
            new("Net sales", "var(--chart-1)", Pts(x => x.Net)),
        };
        var step = Math.Max(1, (int)Math.Ceiling(n / 31.0));
        page.Cols = days.Select((x, i) => new ChartCol(
            x.Weekend,
            i % step == 0 ? WeeklySalesTrendPage.DayNames[(int)x.D.DayOfWeek][..2] + "\n" + x.D.Day : "",
            x.Future
                ? WeeklySalesTrendPage.Dmy(x.D) + " · No data yet"
                : WeeklySalesTrendPage.Dmy(x.D) + " " + WeeklySalesTrendPage.DayNames[(int)x.D.DayOfWeek][..3] + " · Gross " + Fmt.Inr(x.Gross) + " · Cancellation −" + Fmt.Inr(x.Canx) + " · Net " + Fmt.Inr(x.Net)))
            .ToList();
        return page;
    }

    /// <summary>Axis label: ₹1.5L for lakhs, ₹50K for thousands.</summary>
    private static string ChartLabel(double v)
    {
        if (v >= 1e5) return "₹" + Regex.Replace((v / 1e5).ToString("0.00", CultureInfo.InvariantCulture), @"\.?0+$", "") + "L";
        return v != 0 ? "₹" + (v / 1e3).ToString(CultureInfo.InvariantCulture) + "K" : "0";
    }
}
