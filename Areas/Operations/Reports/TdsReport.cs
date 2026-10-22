using SouthernTravels.Areas.Operations.Models;
using SouthernTravels.Core.Infrastructure;
using SouthernTravels.Core.Models;

namespace SouthernTravels.Areas.Operations.Reports;

/// <summary>An expense head with its TDS section and head.</summary>
public sealed record TdsHead(string Name, string Section, string Head);

/// <summary>One vendor / guide bill with the TDS deducted on it.</summary>
public sealed class TdsBill
{
    public static readonly TdsHead[] Heads =
    {
        new("Car Hire expenses", "393(1) · Table Sl. 6(i) D(a)/(b)", "Contractor"),
        new("Food expenses", "393(1) · Table Sl. 6(i) D(a)/(b)", "Contractor"),
        new("Guide Expenses", "393(1) · Table Sl. 6(iii) D(a)", "Profession"),
        new("HAC Commission", "393(1) · Table Sl. 1(ii)", "Commission"),
    };

    public DateTime Bd { get; init; }
    public int HeadIdx { get; init; }
    public required string Vendor { get; init; }
    public required string Pan { get; init; }
    public double Basic { get; init; }
    public int GstPct { get; init; }
    public bool Inter { get; init; }
    public required string Narration { get; init; }

    public TdsHead Head => Heads[HeadIdx];
    public double Gst => Fmt.Round(Basic * GstPct / 100);
    public double Invoice => Basic + Gst;
    /// <summary>TDS rate on the basic amount: 10% profession, 2% commission, else 1% (individual / HUF PAN) or 2%.</summary>
    public double Rate => HeadIdx == 2 ? 0.1 : HeadIdx == 3 ? 0.02 : "PH".Contains(Pan[3]) ? 0.01 : 0.02;
    public string RatePct => (Fmt.Round(Rate * 1000) / 10.0).ToString(System.Globalization.CultureInfo.InvariantCulture);
    public double Tds => Fmt.Round(Basic * Rate);
    public double Net => Basic + Gst - Tds;

    // Display-ready values.
    public string GstText => Gst > 0 ? Fmt.Inr(Gst) : "₹0";
    public string GstSub => Gst > 0 ? (Inter ? "Inter-state" : "Intra-state") : "No GST";
    public string? GstTip => Gst == 0 ? null : Inter ? "IGST " + Fmt.Inr(Gst) : "CGST " + Fmt.Inr(Gst / 2) + " · SGST " + Fmt.Inr(Gst / 2);
    public string TdsSub => "@ " + RatePct + "% of basic";

    private static readonly DateTime Oct1 = new(2026, 10, 1);
    private static DateTime D1(int n) => Oct1.AddDays(n - 1);

    public static readonly IReadOnlyList<TdsBill> All = new TdsBill[]
    {
        new() { Bd = D1(3), HeadIdx = 0, Vendor = "Jai Travels", Pan = "AAHPJ4471K", Basic = 21000, GstPct = 5, Inter = true, Narration = "Airport transfers and city tour · GRP072064" },
        new() { Bd = D1(2), HeadIdx = 0, Vendor = "Royal Cabs Delhi Pvt Ltd", Pan = "AABCR2210L", Basic = 84000, GstPct = 5, Narration = "Delhi–Agra–Jaipur coach hire · GRP072066" },
        new() { Bd = D1(1), HeadIdx = 0, Vendor = "Speed Wheels Travels", Pan = "AAFFS7712D", Basic = 46500, GstPct = 5, Inter = true, Narration = "Tempo traveller, 6 days · GRP072071" },
        new() { Bd = D1(4), HeadIdx = 1, Vendor = "Saravana Bhavan Catering", Pan = "AAJFS1198Q", Basic = 38250, GstPct = 5, Narration = "Group meals, 3 days · GRP072077" },
        new() { Bd = D1(2), HeadIdx = 1, Vendor = "Hotel Annapurna", Pan = "AAACH8841B", Basic = 62400, GstPct = 5, Narration = "Dinner and breakfast package · GRP072080" },
        new() { Bd = D1(5), HeadIdx = 1, Vendor = "Meera Rao Caterers", Pan = "BGKPR3320M", Basic = 18200, GstPct = 5, Inter = true, Narration = "Picnic lunches · GRP072083" },
        new() { Bd = D1(3), HeadIdx = 2, Vendor = "Anil Deshmukh", Pan = "BPDPD5521J", Basic = 25000, GstPct = 0, Narration = "Local guide, Jaipur · GRP072088" },
        new() { Bd = D1(1), HeadIdx = 2, Vendor = "Fatima Sheikh", Pan = "CRMPS9034N", Basic = 18000, GstPct = 0, Inter = true, Narration = "Heritage walk guide · GRP072091" },
        new() { Bd = D1(4), HeadIdx = 2, Vendor = "Ravi Kumar Guides", Pan = "ABEPK6620R", Basic = 32000, GstPct = 0, Narration = "Multi-day guide, Rajasthan · GRP072095" },
        new() { Bd = D1(5), HeadIdx = 3, Vendor = "Skyline Holiday Agents LLP", Pan = "AAQFS5503H", Basic = 56000, GstPct = 18, Inter = true, Narration = "Commission on group booking · GRP072099" },
        new() { Bd = D1(2), HeadIdx = 3, Vendor = "Wanderlust Agents", Pan = "AAECW7714C", Basic = 31500, GstPct = 18, Narration = "Commission on FIT bookings" },
        new() { Bd = Oct1.AddDays(-3), HeadIdx = 0, Vendor = "Jai Travels", Pan = "AAHPJ4471K", Basic = 19500, GstPct = 5, Inter = true, Narration = "Airport transfers · GRP071902" },
    };
}

public sealed class TdsPage : ReportPage
{
    public List<TdsBill> Rows { get; set; } = new();
    public TaxFoot? Foot { get; set; }
}

public static class TdsReport
{
    public static TdsPage Build(ReportQuery q)
    {
        var page = new TdsPage
        {
            Id = "r13",
            Title = "TDS Report",
            Description = "TDS Report — TDS deducted on vendor and guide bills. Shows each bill’s basic amount, GST, invoice value, TDS section and head, the TDS amount at the applicable rate (on basic amount) and the net payable.",
            Query = q,
            SearchPlaceholder = "Search Vendor, PAN, Expense head, Narration...",
            PeriodField = "Date of Bill",
            DefaultPeriod = "This Month",
            PagerUnit = "bills",
            Filters = new FilterDef[]
            {
                new("Month", TaxData.Months),
                new("Expense Head", TdsBill.Heads.Select(h => h.Name).ToArray()),
                new("TDS Head", new[] { "Contractor", "Profession", "Commission" }),
                new("Inter/Intra", new[] { "Inter-state", "Intra-state" }),
            },
            DateFields = new[] { "Date of Bill" },
        };

        var f = TdsBill.All.Where(b => TaxData.InPeriod(b.Bd)).OrderByDescending(b => b.Bd).ToList();

        page.Kpis.AddRange(new[]
        {
            new Kpi("Total bills", f.Count.ToString(), Big: true),
            new Kpi("Basic amount", Fmt.Inr(f.Sum(b => b.Basic)), Big: true),
            new Kpi("GST", Fmt.Inr(f.Sum(b => b.Gst)), Big: true),
            new Kpi("Invoice value", Fmt.Inr(f.Sum(b => b.Invoice)), Big: true),
            new Kpi("TDS deducted", Fmt.Inr(f.Sum(b => b.Tds)), "var(--color-action-blue)", true),
            new Kpi("Net payable", Fmt.Inr(f.Sum(b => b.Net)), "var(--ok-ink)", true),
        });

        if (f.Count > 0)
            page.Foot = new TaxFoot("Totals", new[]
            {
                ("Basic amount", Fmt.Inr(f.Sum(b => b.Basic))),
                ("GST", Fmt.Inr(f.Sum(b => b.Gst))),
                ("Invoice value", Fmt.Inr(f.Sum(b => b.Invoice))),
                ("Net payable", Fmt.Inr(f.Sum(b => b.Net))),
            }, "TDS payable", Fmt.Inr(f.Sum(b => b.Tds)));

        (page.Rows, page.Pager) = Pager.Paginate(f, q.Page);
        return page;
    }
}
