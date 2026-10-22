using SouthernTravels.Areas.Operations.Data;
using SouthernTravels.Core.Infrastructure;
using SouthernTravels.Core.Models;

namespace SouthernTravels.Areas.Operations.Reports;

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
