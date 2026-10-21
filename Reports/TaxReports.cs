using System.Globalization;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

/// <summary>A TCS collected on an earlier (cancelled) ticket that is adjusted against the current one.</summary>
public sealed record PrevTicket(string No, double Tcs, DateTime Dob, DateTime Canx);

/// <summary>Totals bar shown under the table of the TCS / GST / TDS reports.</summary>
public sealed record TaxFoot(string Label, IReadOnlyList<(string Label, string Value)> Items, string TotalLabel, string Total, string TotalColor = "", string TotalNote = "");

/// <summary>
/// One sample international-tour ticket shared by the TCS and GST reports. TCS is taken at 2% of the
/// ticket total (base + GST); "Adj" is the amount the ticket's own TCS differs from that rate.
/// </summary>
public sealed class TaxTicket
{
    public int I { get; init; }
    public required string No { get; init; }
    public DateTime Dob { get; init; }
    public DateTime? Canx { get; init; }
    public double Tv { get; init; }
    public double Disc { get; init; }
    public double Gst { get; init; }
    public required string Pan { get; init; }
    public required string Name { get; init; }
    public DateTime Doj { get; init; }
    public DateTime Doc { get; init; }
    public required string Exec { get; init; }
    public PrevTicket? Prev { get; init; }
    public double Adj { get; init; }
    public string Remark { get; init; } = "";

    public double Base => Tv + Disc;
    public double Total => Tv + Gst;
    /// <summary>TCS at the applicable rate (2% of base + GST).</summary>
    public double Actual => Fmt.Round(Total * 0.02);
    /// <summary>TCS charged on the ticket.</summary>
    public double OnTicket => Actual + Adj;
    public double Diff => OnTicket - Actual;
    public double Final => Prev is null ? OnTicket : Actual - Prev.Tcs;
    public double Grand => Total + OnTicket;
    public string Branch => TaxData.Branches.GetValueOrDefault(Exec, "");

    private static readonly DateTime Oct1 = new(2026, 10, 1);
    private static DateTime D1(int n) => Oct1.AddDays(n - 1);

    /// <summary>The 12 sample tickets (the last one was booked in September).</summary>
    public static readonly IReadOnlyList<TaxTicket> All = new TaxTicket[]
    {
        new() { I = 0, No = "GRP072064", Dob = D1(1), Tv = 229911, Gst = 11496, Pan = "ADEPG7019E", Name = "Gowri Shankar", Doj = D1(13), Doc = D1(21), Exec = "Vineela", Prev = new("GRP071446", 4252, new(2026, 7, 13), D1(1)) },
        new() { I = 1, No = "GRP072066", Dob = D1(1), Tv = 394764, Gst = 19739, Pan = "CXLPS3195Q", Name = "Sarala Nagalingam", Doj = D1(13), Doc = D1(21), Exec = "Vineela", Prev = new("GRP071445", 7392, new(2026, 7, 13), D1(1)) },
        new() { I = 2, No = "GRP072069", Dob = D1(1), Tv = 6209, Gst = 311, Pan = "ABMPC8593R", Name = "C V Chandra Sekhar", Doj = D1(20), Doc = D1(27), Exec = "ramesh.pasargi" },
        new() { I = 3, No = "GRP072071", Dob = D1(1), Tv = 118395, Gst = 5920, Pan = "AGXPN3262G", Name = "Narapusetty Hanumantha Rao", Doj = D1(22), Doc = D1(27), Exec = "p.kalyan" },
        new() { I = 4, No = "GRP072077", Dob = D1(1), Tv = 7209, Gst = 361, Pan = "AINPP6318F", Name = "M Pardhasaradhi", Doj = D1(20), Doc = D1(27), Exec = "ramesh.pasargi" },
        new() { I = 5, No = "GRP072080", Dob = D1(3), Tv = 152400, Gst = 27432, Pan = "BQRPK4412M", Name = "Kavitha Reddy", Doj = D1(18), Doc = D1(25), Exec = "divya.menon", Disc = 2400 },
        new() { I = 6, No = "GRP072083", Dob = D1(5), Tv = 87650, Gst = 4383, Pan = "CTMPS8821D", Name = "Suresh Babu", Doj = D1(24), Doc = D1(30), Exec = "p.kalyan", Prev = new("GRP071902", 1840, new(2026, 8, 20), D1(5)) },
        new() { I = 7, No = "GRP072088", Dob = D1(2), Tv = 312000, Gst = 15600, Pan = "DKLPR2290H", Name = "Meenakshi Iyer", Doj = D1(26), Doc = D1(30), Exec = "Vineela", Adj = -330, Remark = "Collected at 1.90% — short by ₹330" },
        new() { I = 8, No = "GRP072091", Dob = D1(3), Canx = D1(4), Tv = 45300, Gst = 8154, Pan = "EHNPV7745B", Name = "Arvind Kumar", Doj = D1(28), Doc = D1(30), Exec = "ramesh.pasargi", Remark = "Cancelled — TCS to be refunded" },
        new() { I = 9, No = "GRP072095", Dob = D1(4), Tv = 198700, Gst = 9935, Pan = "FGTPM5503K", Name = "Rajesh Menon", Doj = D1(29), Doc = D1(30), Exec = "divya.menon", Prev = new("GRP072001", 3680, D1(2), D1(4)), Disc = 1500 },
        new() { I = 10, No = "GRP072099", Dob = D1(5), Tv = 9800, Gst = 1764, Pan = "GJSPD1167E", Name = "Padma Latha", Doj = D1(30), Doc = D1(30), Exec = "p.kalyan" },
        new() { I = 11, No = "GRP072104", Dob = Oct1.AddDays(-4), Tv = 421500, Gst = 21075, Pan = "HNVPL9034C", Name = "Harish Chandra", Doj = D1(12), Doc = D1(19), Exec = "Vineela", Disc = 5000 },
    };
}

/// <summary>Shared sample-data lookups and display helpers for the three tax reports.</summary>
public static class TaxData
{
    /// <summary>Executive → booking branch.</summary>
    public static readonly IReadOnlyDictionary<string, string> Branches = new Dictionary<string, string>
    {
        ["Vineela"] = "HYD001", ["ramesh.pasargi"] = "DEL001", ["p.kalyan"] = "HYD001", ["divya.menon"] = "BLR001",
    };

    public static readonly string[] Executives = { "Vineela", "ramesh.pasargi", "p.kalyan", "divya.menon" };
    public static readonly string[] Months = { "Oct 2026", "Sep 2026" };

    /// <summary>The Period control defaults to "This Month" (Oct 2026, sample today 5 Oct 2026) for these reports.</summary>
    public static bool InPeriod(DateTime d) => d.Year == 2026 && d.Month == 10;

    /// <summary>"05 Oct 26".</summary>
    public static string Sd(DateTime d) => d.ToString("dd MMM yy", CultureInfo.InvariantCulture);

    /// <summary>"+₹1,200" / "−₹330".</summary>
    public static string Sgn(double v) => (v > 0 ? "+" : "−") + Fmt.Inr(Math.Abs(v));

    public static string Pct2(double v) => v.ToString("F2", CultureInfo.InvariantCulture) + "%";
}
