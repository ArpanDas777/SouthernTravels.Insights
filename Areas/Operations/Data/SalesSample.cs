using SouthernTravels.Core.Infrastructure;

namespace SouthernTravels.Areas.Operations.Data;

/// <summary>
/// Deterministic sample sales figures for Weekly Sales trend, Daily EMP Sales and Sales vs Target,
/// ported from the design prototype's seeded generators. Replace with real sales data when available.
/// </summary>
public static class SalesSample
{
    /// <summary>Last day with sales data in the sample.</summary>
    public static readonly DateTime DataTill = new(2026, 10, 8);

    public static readonly (string Branch, double Weight)[] TrendBranches =
        { ("Delhi", .34), ("Hyderabad", .24), ("Bangalore", .26), ("Chennai", .16) };

    public static readonly (string Type, double Weight)[] TicketTypes =
    {
        ("EBK", .12), ("SPL", .1), ("MBK", .14), ("GRP International FIT", .08), ("GRP International GIT", .1),
        ("GRP International Customized", .06), ("HAC", .08), ("CAB", .05), ("PAB", .05), ("FLT", .1), ("TRN", .04), ("GRP Supplementary", .08),
    };

    public static readonly (string Branch, string Code, (string Name, double FyCrore)[] Emps)[] Employees =
    {
        ("Bangalore Jaynagar", "BLR001", new[] { ("R N Vani", 4.0), ("Shashank G", 2.5), ("Theja Mahadev", 3.5) }),
        ("Bangalore SESH", "BLR002", new[] { ("Gazala Irfan", 4.0), ("K Adinarayana", 4.0) }),
        ("Bangalore YES", "BLR003", new[] { ("Prajwal Rao", 2.5) }),
        ("Chennai Annanagar", "MAA001", new[] { ("Deepa Mohan", 3.0), ("Sangeeta Singh", 4.0) }),
        ("Chennai Periyamet", "MAA002", new[] { ("Karthik Raman", 3.0), ("Lakshmi Narayanan", 2.5) }),
        ("Guntur", "GNT001", new[] { ("Venkat Rao", 3.0), ("Suresh Babu", 3.0) }),
        ("Hyderabad Banjara Hills", "HYD001", new[] { ("Imran Sheikh", 4.0), ("Sunita Iyer", 3.5) }),
        ("Delhi Connaught Place", "DEL001", new[] { ("Anita Rao", 5.0), ("Rohit Sen", 4.0) }),
    };

    private static double Frac(double x) => x - Math.Floor(x);
    private static double R(double seed, double a, double b, int n) => Frac(Math.Sin(seed * a + n * b) * 43758.5453);
    private static double Seed(DateTime d) => d.Year * 372 + (d.Month - 1) * 31 + d.Day;
    public static bool IsWeekend(DateTime d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>Company-wide gross and cancellation for one day.</summary>
    private static (double Gross, double Canx) Day(DateTime d)
    {
        var s = Seed(d);
        var gross = Fmt.Round((IsWeekend(d) ? 380000 + R(s, 12.9898, 78.233, 1) * 320000 : 640000 + R(s, 12.9898, 78.233, 1) * 620000) / 100) * 100;
        var canx = Fmt.Round(gross * (0.04 + R(s, 12.9898, 78.233, 2) * 0.1) / 100) * 100;
        return (gross, canx);
    }

    /// <summary>Gross / cancellation / net for one day, optionally limited to one branch and/or ticket type.</summary>
    public static (double Gross, double Canx, double Net) Trend(DateTime d, string? branch, string? type)
    {
        var b = Day(d);
        var s = Seed(d);
        double gross = 0, canx = 0;
        for (var i = 0; i < TrendBranches.Length; i++)
            for (var j = 0; j < TicketTypes.Length; j++)
            {
                if ((branch is not null && branch != TrendBranches[i].Branch) || (type is not null && type != TicketTypes[j].Type)) continue;
                var w = TrendBranches[i].Weight * TicketTypes[j].Weight * (.8 + .4 * R(s, 4.1414, 91.337, i * 5 + j + 1));
                gross += Fmt.Round(b.Gross * w / 100) * 100;
                canx += Fmt.Round(b.Canx * w * (.7 + .6 * R(s, 4.1414, 91.337, i * 3 + j + 20)) / 100) * 100;
            }
        return (gross, canx, gross - canx);
    }

    /// <summary>One employee's sales on one day (0 on many weekends); fyCrore = annual target in crore.</summary>
    public static double EmpSale(int ei, DateTime d, double fyCrore)
    {
        double seed = ei * 977 + (d.Month - 1) * 31 + d.Day + d.Year * 7;
        if (R(seed, 12.9898, 78.233, 3) < (IsWeekend(d) ? .6 : .1)) return 0;
        return Fmt.Round(fyCrore * 1e7 / 365 * (.55 + ei * 37 % 13 / 13.0 * .8) * (.3 + R(seed, 12.9898, 78.233, 4) * 1.7) / 500) * 500;
    }

    /// <summary>Base-year multiplier used by the Comparison view (period to date).</summary>
    public static double BaseYearFactor(int ei, int yearsBack) =>
        .72 + Frac(Math.Sin((ei * 7 + yearsBack * 13) * 12.9898) * 43758.5453) * .5;
}
