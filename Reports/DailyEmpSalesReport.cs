using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

/// <summary>A day (Day &amp; Week view) or a month (Month &amp; Quarter view) column.</summary>
public sealed record SalesCol(DateTime D, int I, string Label, string Dow, bool Weekend, bool Future, int Quarter);

/// <summary>A week (Day view) or quarter (Month view) of columns; shown as one total column.</summary>
public sealed record SalesWeek(string Key, string Label, List<SalesCol> Cols);

/// <summary>Achievement pill: text with its marker and a tone (ok / warn / bad).</summary>
public sealed record AchCell(string Text, string Tone);

/// <summary>Variation (actual − target) with the achievement % beside it.</summary>
public sealed record VarAchCell(string VarText, string VarCls, string? AchText, string AchCls);

/// <summary>A value with its variance against another figure (Comparison view).</summary>
public sealed record CmpBase(string Text, string VarText, string VarCls);

public sealed class Emp
{
    public required string Name { get; init; }
    public required string Branch { get; init; }
    public int Ei { get; init; }
    public double FyCrore { get; init; }
    public double FyAmt => FyCrore * 1e7;
    public double?[] Vals { get; set; } = Array.Empty<double?>();
    public double Sales { get; set; }
    public double? Target { get; set; }

    // Comparison base year and Sales vs Target figures.
    public (double? T, double S) Base { get; set; }
    public (double T, double G, double C, double N) Yday { get; set; }
    public (double A, double T) Mtd { get; set; }
    public (double A, double T) Qtd { get; set; }
    public (double A, double T) Ytd { get; set; }
}

public sealed record EmpGroup(string Branch, string Code, List<Emp> Emps)
{
    public string Title => Branch + " (" + Code + ")";
}

public static class SalesFmt
{
    /// <summary>Achievement pill tone: ≥100% ok, ≥60% warn, else danger; marker ▲ / ◆ / ▼.</summary>
    public static AchCell? Achv(double s, double? t)
    {
        if (t is null || t == 0) return null;
        var p = s / t.Value * 100;
        return new AchCell((p > 100 ? "▲ " : p == 100 ? "◆ " : "▼ ") + Fmt.Fixed1(p) + "%", p >= 100 ? "ok" : p >= 60 ? "warn" : "bad");
    }

    /// <summary>Variation text and colour, with the achievement % ("/ ▲ 105.0%") when there is a target.</summary>
    public static VarAchCell VarAch(double a, double t)
    {
        var v = a - t;
        var text = v == 0 ? "-" : (v > 0 ? "+" : "−") + Fmt.Inr(Math.Abs(v));
        var cls = "fw6 " + (v == 0 ? "c-mut" : v > 0 ? "c-ok" : "c-dg");
        if (t == 0) return new VarAchCell(text, cls, null, "");
        var p = a / t * 100;
        return new VarAchCell(text, cls, "/ " + (p > 100 ? "▲ " : p == 100 ? "◆ " : "▼ ") + Fmt.Fixed1(p) + "%", p >= 100 ? "c-ok" : p >= 60 ? "c-wd" : "c-dg");
    }

    /// <summary>Variance text "▲ +4.2%" / "▼ −3.0 pp" and its colour class.</summary>
    public static (string Text, string Cls) Var(double? v, string unit)
    {
        if (v is null) return ("", "c-mut");
        var arrow = v > 0 ? "▲ +" : v < 0 ? "▼ −" : "◆ ";
        return (arrow + Fmt.Fixed1(Math.Abs(v.Value)) + unit, v == 0 ? "c-wd" : v > 0 ? "c-ok" : "c-dg");
    }

    public static string Fyl(string y) => "FY" + y[2..] + "–" + (int.Parse(y) + 1).ToString()[2..];
}

/// <summary>One row of the Day &amp; Week / Month &amp; Quarter tables: branch, employee or grand total.</summary>
public sealed record SalesRow(string Kind, string Name, string? Sub, List<double?> Cells, double? Fy, double? Target, double Sales, AchCell? Ach);

/// <summary>One measure line of the Comparison view (Target till date / Sales till date / Achv %).</summary>
public sealed record CmpLine(string Label, string CurrentText, string CurrentCls, AchCell? CurrentAch, CmpBase Base);

/// <summary>A branch, employee or the grand total in the Comparison view, with its three measure lines.</summary>
public sealed record CmpEntity(string Kind, string Name, string? Sub, List<CmpLine> Lines);

public sealed class DailyEmpSalesPage : ReportPage
{
    public bool IsMonthView => ActiveTab == "month";
    public bool IsComparison => ActiveTab == "cmp";
    public string EFrom { get; init; } = "";
    public string ETo { get; init; } = "";
    public string MFrom { get; init; } = "";
    public string MTo { get; init; } = "";
    /// <summary>Weeks (Day view) or quarters (Month view); each is one total column.</summary>
    public List<SalesWeek> Weeks { get; set; } = new();
    public List<SalesRow> Rows { get; set; } = new();
    public List<CmpEntity> CmpRows { get; set; } = new();
    public string BaseYearLabel { get; set; } = "";
    public int ColumnCount => Weeks.Count + 5;
}

public static class DailyEmpSalesReport
{
    private static readonly TabDef[] Tabs = { new("day", "Day & Week View"), new("month", "Month & Quarter View"), new("cmp", "Comparison") };
    private static readonly string[] FyMonths = { "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Jan", "Feb", "Mar" };

    public static readonly FilterDef BranchFilter = new("Branch", SalesSample.Employees.Select(b => b.Branch).ToArray());

    public static DailyEmpSalesPage Build(ReportQuery q)
    {
        var tab = ReportPage.ResolveTab(q, Tabs);
        var page = new DailyEmpSalesPage
        {
            Id = "r8",
            Title = "Daily EMP Sales",
            Description = "Daily EMP Sales — employee-wise daily sales by branch for the selected period, with the current-year target, target till date, sales till date and achievement %.",
            Query = q,
            SearchPlaceholder = "Search employees or branches",
            HasExport = false,
            Tabs = Tabs,
            ActiveTab = tab,
            CompactTabs = true,
            KpiMin = 118,
            Filters = new[] { BranchFilter },
            TitleControls = "_DailyEmpSalesControls",
            ToolbarExtra = tab == "cmp" ? "_ComparisonControls" : null,
            EFrom = "2026-10-01",
            ETo = "2026-10-08",
            MFrom = "2026-04",
            MTo = "2027-03",
        };

        var etd = SalesSample.DataTill;
        var mv = page.IsMonthView;
        var cols = new List<SalesCol>();

        // Columns, grouped into weeks (Day view) or quarters (Month view).
        if (mv)
        {
            for (var i = 0; i < 12; i++)
            {
                var d = new DateTime(2026, 4, 1).AddMonths(i);
                cols.Add(new SalesCol(d, cols.Count, FyMonths[i], "", false, d > etd, i / 3));
            }
            page.Weeks = cols.GroupBy(c => c.Quarter).Select(g => new SalesWeek(g.Key.ToString(), "Q" + (g.Key + 1), g.ToList())).ToList();
        }
        else
        {
            for (var d = new DateTime(2026, 10, 1); d <= new DateTime(2026, 10, 8); d = d.AddDays(1))
                cols.Add(new SalesCol(d, cols.Count, d.Day.ToString("D2"), new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" }[(int)d.DayOfWeek], SalesSample.IsWeekend(d), d > etd, 0));
            page.Weeks = cols.GroupBy(c => c.D.AddDays(-(((int)c.D.DayOfWeek + 6) % 7)))
                .Select((g, wi) => new SalesWeek(Fmt.Iso(g.Key), "Wk" + (wi + 1), g.ToList())).ToList();
        }

        // Employees, their daily / monthly sales and target till date.
        var elapsed = mv ? (int)Math.Round((etd - new DateTime(2026, 4, 1)).TotalDays) + 1 : cols.Count(c => !c.Future);
        var groups = BuildGroups((ei, fy) => cols.Select(c => c.Future ? (double?)null : mv ? MonthSale(ei, c.D, fy) : SalesSample.EmpSale(ei, c.D, fy)).ToArray(), elapsed);
        var all = groups.SelectMany(g => g.Emps).ToList();

        if (page.IsComparison) BuildComparison(page, groups, all);
        else page.Rows = BuildRows(page, groups, all, cols);

        // KPIs
        var tgt = all.Sum(e => e.Target ?? 0);
        var sales = all.Sum(e => e.Sales);
        var pc = tgt > 0 ? sales / tgt * 100 : 0;
        var withT = all.Where(e => e.Target is > 0).ToList();
        var top = withT.OrderByDescending(e => e.Sales / e.Target!.Value).FirstOrDefault();
        page.Kpis.AddRange(new[]
        {
            new Kpi("Branches", groups.Count.ToString(), Big: true),
            new Kpi("Employees", all.Count.ToString(), Big: true),
            new Kpi("Target (Current Year)", Fmt.Inr(all.Sum(e => e.FyAmt))),
            new Kpi("Target till date", Fmt.Inr(tgt), "var(--color-action-blue)"),
            new Kpi("Sales till date", Fmt.Inr(sales), "var(--ok-ink)"),
            new Kpi("Achievement %", tgt > 0 ? Fmt.Fixed1(pc) + "%" : "-", pc >= 100 ? "var(--ok-ink)" : pc >= 60 ? "var(--warn-ink-deep)" : "var(--color-danger)", true),
            new Kpi("Achieved ≥100%", withT.Count(e => e.Sales >= e.Target).ToString(), "var(--ok-ink)", true, "of " + all.Count),
            new Kpi("Below 60%", withT.Count(e => e.Sales / e.Target!.Value < .6).ToString(), "var(--color-danger)", true, "of " + all.Count),
            new Kpi("Top achiever", top?.Name ?? "-", Sub: top is null ? null : Fmt.Fixed1(top.Sales / top.Target!.Value * 100) + "%"),
        });
        page.ResultLabel = all.Count + " employees";
        return page;
    }

    /// <summary>Employees grouped by branch.</summary>
    public static List<EmpGroup> BuildGroups(Func<int, double, double?[]> vals, int elapsed)
    {
        var ei = 0;
        var groups = new List<EmpGroup>();
        foreach (var (bn, code, emps) in SalesSample.Employees)
        {
            var es = new List<Emp>();
            foreach (var (name, fy) in emps)
            {
                var e = new Emp { Name = name, Branch = bn, Ei = ei++, FyCrore = fy };
                e.Vals = vals(e.Ei, fy);
                e.Sales = e.Vals.Sum(v => v ?? 0);
                e.Target = elapsed > 0 ? Fmt.Round(fy * 1e7 * elapsed / 365 / 1000) * 1000 : null;
                es.Add(e);
            }
            groups.Add(new EmpGroup(bn, code, es));
        }
        return groups;
    }

    private static double MonthSale(int ei, DateTime monthStart, double fy)
    {
        double s = 0;
        for (var k = monthStart; k.Month == monthStart.Month && k <= SalesSample.DataTill; k = k.AddDays(1)) s += SalesSample.EmpSale(ei, k, fy);
        return s;
    }

    /// <summary>Branch / employee / grand-total rows with one value per week (or quarter) column.</summary>
    private static List<SalesRow> BuildRows(DailyEmpSalesPage page, List<EmpGroup> groups, List<Emp> all, List<SalesCol> cols)
    {
        List<double?> WeekValues(double?[] vals) => page.Weeks.Select(w => w.Cols.All(c => c.Future) ? (double?)null : w.Cols.Sum(c => vals[c.I] ?? 0)).ToList();
        double?[] Sum(IEnumerable<Emp> es) => cols.Select(c => c.Future ? (double?)null : es.Sum(e => e.Vals[c.I] ?? 0)).ToArray();
        SalesRow Row(string kind, string name, string? sub, double?[] vals, double fy, double? target, double sales) =>
            new(kind, name, sub, WeekValues(vals), fy, target, sales, SalesFmt.Achv(sales, target));

        var rows = new List<SalesRow>();
        foreach (var g in groups)
        {
            rows.Add(Row("grp", "▾ " + g.Title, null, Sum(g.Emps), g.Emps.Sum(e => e.FyAmt), g.Emps.Sum(e => e.Target ?? 0), g.Emps.Sum(e => e.Sales)));
            rows.AddRange(g.Emps.Select(e => Row("emp", e.Name, e.Branch, e.Vals, e.FyAmt, e.Target, e.Sales)));
        }
        if (all.Count > 0) rows.Add(Row("gsum", "GRAND TOTAL", null, Sum(all), all.Sum(e => e.FyAmt), all.Sum(e => e.Target ?? 0), all.Sum(e => e.Sales)));
        return rows;
    }

    /// <summary>Comparison, period to date: this year against the FY25–26 base year.</summary>
    private static void BuildComparison(DailyEmpSalesPage page, List<EmpGroup> groups, List<Emp> all)
    {
        page.BaseYearLabel = SalesFmt.Fyl("2025");
        foreach (var e in all)
        {
            const int yearsBack = 1;
            e.Base = (e.Target is > 0 ? Fmt.Round(e.Target.Value * (1 - .05 * yearsBack) / 1000) * 1000 : null,
                      Fmt.Round(e.Sales * SalesSample.BaseYearFactor(e.Ei, yearsBack) / 500) * 500);
        }

        string Fmv(double? v, bool pct) => v is null ? "-" : pct ? Fmt.Fixed1(v.Value) + "%" : Fmt.Inr(v.Value);
        double? Pct((double? T, double S) x) => x.T is > 0 ? x.S / x.T.Value * 100 : null;

        CmpEntity Entity(string kind, string name, string? sub, (double? T, double S) cur, (double? T, double S) bas)
        {
            var lines = new List<CmpLine>();
            foreach (var (k, label) in new[] { ("t", "Target till date"), ("s", "Sales till date"), ("a", "Achv %") })
            {
                double? Val((double? T, double S) x) => k == "a" ? Pct(x) : k == "t" ? x.T : x.S;
                var cu = Val(cur);
                var b2 = Val(bas);
                double? d = cu is not null && b2 is not null ? (k == "a" ? cu - b2 : b2 == 0 ? null : (cu - b2) / b2 * 100) : null;
                var v = SalesFmt.Var(d, k == "a" ? " pp" : "%");
                lines.Add(new CmpLine(label, Fmv(cu, false), k == "s" ? "fw7" : "fw5", k == "a" ? SalesFmt.Achv(cur.S, cur.T) : null, new CmpBase(Fmv(b2, k == "a"), v.Text, v.Cls)));
            }
            return new CmpEntity(kind, name, sub, lines);
        }

        (double? T, double S) Tot(IEnumerable<Emp> es) => (es.Sum(e => e.Target ?? 0), es.Sum(e => e.Sales));
        (double? T, double S) BaseTot(IEnumerable<Emp> es) => (es.Sum(e => e.Base.T ?? 0), es.Sum(e => e.Base.S));

        foreach (var g in groups)
        {
            page.CmpRows.Add(Entity("grp", "▾ " + g.Title, null, Tot(g.Emps), BaseTot(g.Emps)));
            page.CmpRows.AddRange(g.Emps.Select(e => Entity("emp", e.Name, e.Branch, (e.Target, e.Sales), e.Base)));
        }
        if (all.Count > 0) page.CmpRows.Add(Entity("gsum", "GRAND TOTAL", null, Tot(all), BaseTot(all)));
    }
}

/// <summary>One row of the Sales vs Target table: branch, executive or grand total.</summary>
public sealed record TargetRow(string Kind, string Name, double Target, double Sales, double Cancellations, double Net, VarAchCell YVar,
    double MAct, double MTgt, VarAchCell MVar, double QAct, double QTgt, VarAchCell QVar, double YAct, double YTgt, VarAchCell YtdVar);

public sealed class SalesVsTargetPage : ReportPage
{
    public string AsOn { get; set; } = "";
    public string DayLabel { get; set; } = "";
    public string MtdLabel { get; set; } = "";
    public string QtdLabel { get; set; } = "";
    public string YtdLabel { get; set; } = "";
    public List<TargetRow> Rows { get; set; } = new();
}

public static class SalesVsTargetReport
{
    public static SalesVsTargetPage Build(ReportQuery q)
    {
        var etd = SalesSample.DataTill;
        var aod = etd;
        var fyS = new DateTime(aod.Month >= 4 ? aod.Year : aod.Year - 1, 4, 1);
        var mS = new DateTime(aod.Year, aod.Month, 1);
        var qS = new DateTime(fyS.Year, 1, 1).AddMonths((aod.Month - 1 + 9) % 12 / 3 * 3 + 3);
        string Short(DateTime d) => d.Day.ToString("D2") + " " + d.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture);

        var page = new SalesVsTargetPage
        {
            Id = "r9",
            Title = "Sales vs Target",
            Description = "Sales vs Target — sales done by each executive: the selected day (Target, Sales, Cancellations, Net sales, Variation, Achieved) and MTD, QTD and YTD (Actual, Target, Var, Achv %). Actual is net sales; targets are the annual target ÷ 365 per day elapsed. Financial year runs Apr–Mar.",
            Query = q,
            SearchPlaceholder = "Search executives or branches",
            HasExport = false,
            KpiMin = 130,
            Filters = new[] { DailyEmpSalesReport.BranchFilter },
            TitleControls = "_SalesVsTargetControls",
            AsOn = Fmt.Iso(aod),
            DayLabel = "Yesterday · " + Short(aod) + " " + (aod.Year % 100).ToString("D2"),
            MtdLabel = "MTD · " + Short(mS) + " – " + Short(aod),
            QtdLabel = "QTD · " + Short(qS) + " – " + Short(aod),
            YtdLabel = "YTD · " + Short(fyS) + " – " + Short(aod),
        };

        var groups = DailyEmpSalesReport.BuildGroups((_, _) => Array.Empty<double?>(), 0);
        var all = groups.SelectMany(g => g.Emps).ToList();
        (double G, double C) G1(Emp e, DateTime d)
        {
            var gr = SalesSample.EmpSale(e.Ei, d, e.FyCrore);
            return (gr, gr != 0 ? Fmt.Round(gr * (.03 + (e.Ei * 7 + d.Day) % 9 / 100.0) / 500) * 500 : 0);
        }
        (double A, double T) Span(Emp e, DateTime s)
        {
            double g = 0, c = 0;
            for (var d = s; d <= aod; d = d.AddDays(1)) { var x = G1(e, d); g += x.G; c += x.C; }
            return (g - c, Fmt.Round(e.FyAmt * ((int)Math.Round((aod - s).TotalDays) + 1) / 365 / 1000) * 1000);
        }
        foreach (var e in all)
        {
            var x = G1(e, aod);
            e.Yday = (Fmt.Round(e.FyAmt / 365 / 500) * 500, x.G, x.C, x.G - x.C);
            e.Mtd = Span(e, mS); e.Qtd = Span(e, qS); e.Ytd = Span(e, fyS);
        }

        TargetRow Row(string kind, string name, IEnumerable<Emp> es)
        {
            var list = es.ToList();
            var y = (T: list.Sum(e => e.Yday.T), G: list.Sum(e => e.Yday.G), C: list.Sum(e => e.Yday.C), N: list.Sum(e => e.Yday.N));
            var m = (A: list.Sum(e => e.Mtd.A), T: list.Sum(e => e.Mtd.T));
            var qq = (A: list.Sum(e => e.Qtd.A), T: list.Sum(e => e.Qtd.T));
            var yy = (A: list.Sum(e => e.Ytd.A), T: list.Sum(e => e.Ytd.T));
            return new TargetRow(kind, name, y.T, y.G, y.C, y.N, SalesFmt.VarAch(y.N, y.T),
                m.A, m.T, SalesFmt.VarAch(m.A, m.T), qq.A, qq.T, SalesFmt.VarAch(qq.A, qq.T), yy.A, yy.T, SalesFmt.VarAch(yy.A, yy.T));
        }
        foreach (var g in groups)
        {
            page.Rows.Add(Row("grp", "▾ " + g.Title, g.Emps));
            page.Rows.AddRange(g.Emps.Select(e => Row("emp", e.Name, new[] { e })));
        }
        if (all.Count > 0) page.Rows.Add(Row("gsum", "GRAND TOTAL", all));

        double Pa(Func<Emp, (double A, double T)> k) { var a = all.Sum(e => k(e).A); var t = all.Sum(e => k(e).T); return t != 0 ? a / t * 100 : 0; }
        string Ac(double v) => v >= 100 ? "var(--ok-ink)" : v >= 60 ? "var(--warn-ink-deep)" : "var(--color-danger)";
        var ex = all.Where(e => e.Mtd.T != 0).Select(e => (e.Name, P: e.Mtd.A / e.Mtd.T * 100)).OrderByDescending(x => x.P).ToList();
        var yTot = (T: all.Sum(e => e.Yday.T), N: all.Sum(e => e.Yday.N));
        page.Kpis.AddRange(new[]
        {
            new Kpi("Yesterday net sales", Fmt.Inr(yTot.N), Big: true, Sub: "Target " + Fmt.Inr(yTot.T)),
            new Kpi("MTD achv %", Fmt.Fixed1(Pa(e => e.Mtd)) + "%", Ac(Pa(e => e.Mtd)), true),
            new Kpi("QTD achv %", Fmt.Fixed1(Pa(e => e.Qtd)) + "%", Ac(Pa(e => e.Qtd)), true),
            new Kpi("YTD achv %", Fmt.Fixed1(Pa(e => e.Ytd)) + "%", Ac(Pa(e => e.Ytd)), true),
            new Kpi("Top executive (MTD)", ex.Count > 0 ? ex[0].Name : "-", Sub: ex.Count > 0 ? Fmt.Fixed1(ex[0].P) + "%" : null),
            new Kpi("Below 60% (MTD)", ex.Count(x => x.P < 60).ToString(), "var(--color-danger)", true, "of " + ex.Count),
        });
        page.ResultLabel = all.Count + " executives";
        return page;
    }
}
