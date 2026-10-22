using System.Globalization;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Reports;

/// <summary>A service voucher listed in the GST drill-down.</summary>
public sealed record GstVoucher(string Service, string No, string Ops, string Branch);

/// <summary>One ticket row of the GST Report with its display-ready values and hover notes.</summary>
public sealed class GstRow
{
    private static readonly string[] Places = { "Telangana", "Telangana", "Karnataka", "Tamil Nadu", "Delhi", "Maharashtra", "Delhi", "Karnataka", "Delhi", "Telangana", "Delhi", "Tamil Nadu" };
    private static readonly double[] AdvPct = { 1, .5, 1, .4, 1, .6, .5, 1, 1, .7, 1, .5 };
    private static readonly string[] Holders = { "Sri Lakshmi Traders", "", "Chandra Enterprises", "", "Rao & Sons", "", "Padma Exports", "", "Kumar Associates", "", "Menon Holdings", "" };
    private static readonly string[] Modes = { "UPI", "Bank transfer", "Card", "Cash", "Cheque" };
    private static readonly string[] Tours = { "Europe Highlights 8N/9D", "Dubai Delight 5N/6D", "Singapore & Malaysia 6N/7D", "Bali Escape 5N/6D", "Thailand Fun 4N/5D", "Switzerland Classic 7N/8D" };
    private static readonly string[] Svc = { "Hotel", "Cab", "Flight", "Visa", "Sightseeing" };
    private static readonly string[] Ops = { "Priya Nair", "Karan Shah", "Sowmya Rao", "Divya Menon", "Arjun Verma" };
    private static readonly string[] OpsBranch = { "DEL001", "HYD001", "BLR001", "MAA001" };
    private static readonly Dictionary<string, string> BranchState = new() { ["HYD001"] = "Telangana", ["DEL001"] = "Delhi", ["BLR001"] = "Karnataka" };
    private static readonly Dictionary<string, string> StateCode = new() { ["Telangana"] = "36", ["Karnataka"] = "29", ["Tamil Nadu"] = "33", ["Delhi"] = "07", ["Maharashtra"] = "27" };

    public required TaxTicket T { get; init; }
    /// <summary>Previous-month advance (excl. GST) when the ticket replaces an earlier one.</summary>
    public double PrevAdvance { get; init; }

    private int I => T.I;
    public string Place => Places[I];
    public bool Inter => Place != BranchState[T.Branch];
    public string StateType => Inter ? "Inter-state" : "Intra-state";
    public double Rate => Fmt.Round(T.Gst / T.Tv * 100);
    public double Advance => Fmt.Round(T.Grand * AdvPct[I]);
    public double Igst => Inter ? Fmt.Round(Advance * Rate / 100) : 0;
    public double Cgst => Inter ? 0 : Fmt.Round(Advance * Rate / 100 / 2);
    public double Balance => T.Grand - Advance;
    public double PrevIgst => Inter ? Fmt.Round(PrevAdvance * Rate / 100) : 0;
    public double PrevCgst => Inter ? 0 : Fmt.Round(PrevAdvance * Rate / 100 / 2);

    public string Holder => Holders[I];
    public string GstNo => Holder.Length > 0 ? StateCode[Place] + T.Pan + "1Z5" : "";
    public string Mobile => "98" + (10000000L + I * 3737371L % 89999999L).ToString(CultureInfo.InvariantCulture).PadLeft(8, '0');
    public string MobileMasked => Mobile[..2] + "••••••" + Mobile[^2..];
    public string PayMode => Modes[I % 5];
    public string Tour => Tours[I % 6];

    public IReadOnlyList<GstVoucher> Vouchers => Enumerable.Range(0, 2 + I % 3)
        .Select(k => new GstVoucher(Svc[(I + k) % 5], "V-26-" + (1000 + I * 10 + k), Ops[(I + k) % 5], OpsBranch[(I + 2 * k) % 4]))
        .ToList();

    // Display-ready values.
    public bool Cancelled => T.Canx is not null;
    public string Booked => "B " + TaxData.Sd(T.Dob);
    public string Journey => "J " + TaxData.Sd(T.Doj);
    public string Completion => "C " + TaxData.Sd(T.Doc);
    public string DiscountText => T.Disc > 0 ? "−" + Fmt.Inr(T.Disc) + " disc." : "₹0 disc.";
    public string GstTip => Inter ? "IGST " + Fmt.Inr(Igst) : "CGST " + Fmt.Inr(Cgst) + " · SGST " + Fmt.Inr(Cgst);
    public string HolderText => Holder.Length > 0 ? Holder + " · " + MobileMasked : "–";
    public string GstNoText => GstNo.Length > 0 ? GstNo : "–";
    private static string Money(double n) => n > 0 ? Fmt.Inr(n) : "–";
    public string PrevAdvanceText => Money(PrevAdvance);
    public string PrevIgstText => Money(PrevIgst);
    public string PrevCgstText => Money(PrevCgst);
}

public sealed class GstPage : ReportPage
{
    public List<GstRow> Rows { get; set; } = new();
    public TaxFoot? Foot { get; set; }
}

public static class GstReport
{
    private static readonly double[] PrevAdv = { 100000, 60000, 40000, 75000 };

    public static GstPage Build(ReportQuery q)
    {
        var page = new GstPage
        {
            Id = "r12",
            Title = "GST Report",
            Description = "GST Report — GST on tour tickets, ticket by ticket. Shows taxable value, GST with TCS beneath it, gross value, the advance received and the balance due. Hover the GST amount for the IGST or CGST + SGST split. Expand a row for the GST holder, GST number, PAN, place of supply, payment mode, previous-month advance and the voucher numbers of every service.",
            Query = q,
            SearchPlaceholder = "Search Ticket No., Passenger, PAN, Voucher, GST No...",
            PeriodField = "DOB",
            DefaultPeriod = "This Month",
            PagerUnit = "tickets",
            Filters = new FilterDef[]
            {
                new("Month", TaxData.Months),
                new("Executive", TaxData.Executives),
                new("Branch", new[] { "DEL001", "HYD001", "BLR001" }),
                new("Status", new[] { "Confirmed", "Cancelled" }),
                new("Inter/Intra", new[] { "Inter-state", "Intra-state" }),
                new("Payment Mode", new[] { "UPI", "Bank transfer", "Card", "Cash", "Cheque" }),
            },
            DateFields = new[] { "DOB" },
        };

        // The previous-month advance is handed out in order to the tickets that replace an earlier one.
        var pi = 0;
        var all = TaxTicket.All.Select(t => new GstRow { T = t, PrevAdvance = t.Prev is null ? 0 : PrevAdv[pi++ % 4] }).ToList();
        var f = all.Where(r => TaxData.InPeriod(r.T.Dob)).OrderByDescending(r => r.T.Dob).ToList();

        static string Compact(double n) => "₹" + (n >= 1e5 ? (n / 1e5).ToString("F2", CultureInfo.InvariantCulture) + "L" : (n / 1e3).ToString("F1", CultureInfo.InvariantCulture) + "K");
        var igst = f.Sum(r => r.Igst);
        var cgst = f.Sum(r => r.Cgst);

        page.Kpis.AddRange(new[]
        {
            new Kpi("Total tickets", f.Count.ToString(), Big: true),
            new Kpi("Taxable value", Fmt.Inr(f.Sum(r => r.T.Tv)), Big: true),
            new Kpi("GST", Fmt.Inr(f.Sum(r => r.T.Gst)), Big: true),
            new Kpi("Gross value", Fmt.Inr(f.Sum(r => r.T.Grand)), Big: true),
            new Kpi("Advance received", Fmt.Inr(f.Sum(r => r.Advance)), "var(--color-action-blue)", true),
            new Kpi("Balance", Fmt.Inr(f.Sum(r => r.Balance)), "var(--alert-ink)", true),
            new Kpi("IGST / CGST / SGST", Compact(igst) + " / " + Compact(cgst) + " / " + Compact(cgst),
                Tip: "IGST " + Fmt.Inr(igst) + " · CGST " + Fmt.Inr(cgst) + " · SGST " + Fmt.Inr(cgst)),
        });

        if (f.Count > 0)
            page.Foot = new TaxFoot("Totals", new[]
            {
                ("Taxable value", Fmt.Inr(f.Sum(r => r.T.Tv))),
                ("GST", Fmt.Inr(f.Sum(r => r.T.Gst))),
                ("TCS", Fmt.Inr(f.Sum(r => r.T.OnTicket))),
                ("Gross value", Fmt.Inr(f.Sum(r => r.T.Grand))),
                ("Advance", Fmt.Inr(f.Sum(r => r.Advance))),
            }, "Balance due", Fmt.Inr(f.Sum(r => r.Balance)));

        (page.Rows, page.Pager) = Pager.Paginate(f, q.Page);
        return page;
    }
}
