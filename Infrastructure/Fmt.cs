using System.Globalization;

namespace SouthernTravels.Insights.Infrastructure;

public static class Fmt
{

    private static readonly string[] Mon = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    /// <summary>JavaScript Math.round semantics (half rounds up).</summary>
    public static long Round(double v) => (long)Math.Floor(v + 0.5);

    /// <summary>"05 Oct 2026".</summary>
    public static string Date(DateTime d) => d.Day.ToString("D2") + " " + Mon[d.Month - 1] + " " + d.Year;

    /// <summary>Drill-down date format: "05/10/26".</summary>
    public static string Short(DateTime d) => d.ToString("dd/MM/yy", CultureInfo.InvariantCulture);

    public static string Short(DateTime? d, string fallback) => d is null ? fallback : Short(d.Value);

    /// <summary>Booking branch → branch code.</summary>
    public static string BranchCode(string branch) => branch switch { "Delhi" => "DEL001", "Hyderabad" => "HYD001", _ => branch };

    /// <summary>₹ with Indian digit grouping, e.g. ₹1,48,500.</summary>
    public static string Inr(double n)
    {
        var v = Round(n);
        var neg = v < 0;
        var s = Math.Abs(v).ToString(CultureInfo.InvariantCulture);
        if (s.Length > 3)
        {
            var last3 = s[^3..];
            var rest = s[..^3];
            var parts = new List<string>();
            while (rest.Length > 2) { parts.Insert(0, rest[^2..]); rest = rest[..^2]; }
            if (rest.Length > 0) parts.Insert(0, rest);
            s = string.Join(",", parts) + "," + last3;
        }
        return "₹" + (neg ? "-" : "") + s;
    }

    /// <summary>Signed variation, e.g. +₹2,400 / −₹1,000.</summary>
    public static string Signed(double v) => (v >= 0 ? "+" : "−") + Inr(Math.Abs(v));

    public static string Fixed1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    public static string Pct0(double v) => Round(v) + "%";

    public static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

}
