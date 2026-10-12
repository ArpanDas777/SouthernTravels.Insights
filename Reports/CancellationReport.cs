using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Reports;

/// <summary>Vendor-side settlement for one cancelled service.</summary>
public sealed class CanxService
{
    public required Service S { get; init; }
    public string VendorName => SampleData.VendorName(S.VendorId);
    public double Advance { get; init; }
    public double Final { get; init; }
    public double RefundFromVendor => Math.Max(0, Advance - Final);
}

/// <summary>One cancelled ticket with its customer charge, override and vendor settlements.</summary>
public sealed class Cancellation
{
    public required Ticket T { get; init; }
    public int K { get; init; }
    public int PolicyPct { get; init; }
    public int VendorPct { get; init; }
    public double PolicyCharge { get; init; }
    public bool Override { get; init; }
    public int OverridePct => PolicyPct - 10;
    public double OverrideCharge { get; init; }
    public double Reduction => PolicyCharge - OverrideCharge;
    public double BaseCharge => Override ? OverrideCharge : PolicyCharge;
    public double Gst => Fmt.Round(BaseCharge * .05);
    public double Total => BaseCharge + Gst;
    public double PaidByCustomer { get; init; }
    public required string Type { get; init; }
    public double Refund => Type == "Refund" ? Math.Max(0, PaidByCustomer - Total) : 0;
    public required List<CanxService> Services { get; init; }
    public double VendorFinal => Services.Sum(v => v.Final);
    public double VendorPayable => Services.Sum(v => v.S.Payable);
    public double Profit => Total - VendorFinal;
    public double ProfitPct => Total != 0 ? Profit / Total * 100 : 0;
    public DateTime CancelDate { get; init; }
    public int DaysLeft => (int)Fmt.Round((T.Start - CancelDate).TotalDays);
    public string Txn => "CX-26-" + (K + 1).ToString("D3");
    public required string Reason { get; init; }
    public string Approver => "Vijay Gupta";
    public string OverrideOn => Fmt.Short(CancelDate) + ", 11:20";
    public double OriginalExGst => T.Grand - Fmt.Round(T.Grand * 5 / 105);
    public double FileProfit => T.Grand - VendorPayable;
    public double FileProfitPct => FileProfit / T.Grand * 100;
    public string RefundOrReissueAmount => Fmt.Inr(Type == "Refund" ? Refund : T.Grand);
    public string ProfitPctText => Fmt.Fixed1(ProfitPct) + "%";
    public string FileProfitPctText => Fmt.Fixed1(FileProfitPct) + "%";

    /// <summary>Days-left pill tone: ≤7 red, ≤14 yellow, otherwise teal.</summary>
    public string DaysLeftTone => DaysLeft <= 7 ? "dl-red" : DaysLeft <= 14 ? "dl-yellow" : "dl-teal";
}

public sealed record CanxVendorItem(Cancellation X, CanxService V)
{
    /// <summary>Override charge on this service (null when the ticket has no override).</summary>
    public double? OverrideAmount => X.Override ? Fmt.Round(V.S.Payable * (X.VendorPct - 10) / 100) : null;
    public string OverridePctText => Math.Max(0, X.VendorPct - 10) + "%";
    public double FileProfit => V.S.Sell - V.S.Payable;
    public string FileProfitPct => Fmt.Fixed1(FileProfit / V.S.Sell * 100) + "%";
}

public sealed record CanxVendorGroup(string Id, List<CanxVendorItem> Items)
{
    public string Name => SampleData.VendorName(Id);
    public IEnumerable<Cancellation> Tickets => Items.Select(a => a.X).DistinctBy(x => x.T.No);
    public IEnumerable<string> Types => Items.Select(a => a.V.S.Type).Distinct();
    public string Cities => string.Join(", ", Items.Select(a => a.V.S.City).Distinct());
    public int TourDays => Tickets.Sum(x => x.T.Days);
    public int TicketCount => Tickets.Count();
    public double Payable => Items.Sum(a => a.V.S.Payable);
    public double Advance => Items.Sum(a => a.V.Advance);
    public double Final => Items.Sum(a => a.V.Final);
    public double Refund => Items.Sum(a => a.V.RefundFromVendor);
}

public sealed class CancellationPage : ReportPage
{
    public bool IsTicketView => ActiveTab == "ticket";
    public List<Cancellation> Rows { get; set; } = new();
    public List<CanxVendorGroup> VendorRows { get; set; } = new();
}

public static class CancellationReport
{
    private static readonly TabDef[] Tabs = { new("ticket", "Ticket-wise View"), new("vendor", "Vendor-wise View") };
    private static readonly string[] Reasons = { "Customer request", "Medical emergency", "Flight cancelled", "Visa rejected", "Change of plans" };

    public static CancellationPage Build(ReportQuery q)
    {
        var page = new CancellationPage
        {
            Id = "r5",
            Title = "Cancellation Report",
            Description = "Cancellation Report — complete details of all cancellations, override approvals, and financial impact at file and service level, including customer-end and vendor-end breakdowns, refund/reissue tracking, and profit analysis per cancellation.",
            Query = q,
            SearchPlaceholder = "Search MBK, Ticket, Passenger, File No., Canx Txn No., Vendor...",
            PeriodField = "Cancellation Date",
            Tabs = Tabs,
            ActiveTab = ReportPage.ResolveTab(q, Tabs),
            Filters = new FilterDef[]
            {
                new("Booking Executive", CommonFilters.Execs),
                new("Booking Branch", CommonFilters.Branches),
                new("Canx Reason", Reasons),
                new("Refund / Reissue", new[] { "Refund", "Reissue" }),
            },
            DateFields = new[] { "Cancellation Date" },
        };

        var f = Build();

        var sales = f.Sum(x => x.T.Grand);
        var collected = f.Sum(x => x.Total);
        var refunds = f.Where(x => x.Type == "Refund").ToList();
        var reissues = f.Where(x => x.Type == "Reissue").ToList();
        page.Kpis.AddRange(new[]
        {
            new Kpi("Total Cancellations", f.Count.ToString(), Big: true),
            new Kpi("Pax Affected", f.Sum(x => x.T.Pax).ToString(), Big: true),
            new Kpi("Total Sales Value", Fmt.Inr(sales), "var(--color-action-blue)"),
            new Kpi("Canx Collected (Cust.)", Fmt.Inr(collected), "var(--color-danger)"),
            new Kpi("Business Lost", Fmt.Inr(sales - collected), "var(--warn-ink-deep)"),
            new Kpi("Total Refund Amt.", Fmt.Inr(refunds.Sum(x => x.Refund)), "var(--ok-ink)", false, refunds.Count + " refund(s)"),
            new Kpi("Total Re-issue Amt.", Fmt.Inr(reissues.Sum(x => x.T.Grand)), "var(--color-action-blue)", false, reissues.Count + " reissue(s)"),
            new Kpi("Canx Given to Vendor", Fmt.Inr(f.Sum(x => x.VendorFinal)), "var(--warn-ink-deep)"),
            new Kpi("Refund from Vendor", Fmt.Inr(f.Sum(x => x.Services.Sum(v => v.RefundFromVendor))), "var(--alert-ink)"),
            new Kpi("Avg. Days Before Cancellation", f.Count > 0 ? Fmt.Round(f.Sum(x => x.DaysLeft) / (double)f.Count) + "d" : "-"),
        });
        page.ResultLabel = f.Count + " cancellations";

        if (page.IsTicketView) (page.Rows, page.Pager) = Pager.Paginate(f, q.Page);
        else
            (page.VendorRows, page.Pager) = Pager.Paginate(f.SelectMany(x => x.Services.Select(v => new CanxVendorItem(x, v)))
                .GroupBy(a => a.V.S.VendorId).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new CanxVendorGroup(g.Key, g.ToList())).ToList(), q.Page);
        return page;
    }

    private static List<Cancellation> Build()
    {
        int[] pcts = { 25, 50, 15, 40, 30 }, vps = { 20, 30, 10, 25, 15 }, before = { 20, 12, 6, 25, 9 };
        return SampleData.Tickets.Where(t => t.Status == "Cancelled").Select((t, k) =>
        {
            var pct = pcts[k % 5];
            var vp = vps[k % 5];
            return new Cancellation
            {
                T = t,
                K = k,
                PolicyPct = pct,
                VendorPct = vp,
                PolicyCharge = Fmt.Round(t.Grand * pct / 100),
                Override = k % 2 == 1,
                OverrideCharge = Fmt.Round(t.Grand * (pct - 10) / 100),
                PaidByCustomer = t.Paid != 0 ? t.Paid : Fmt.Round(t.Grand * .5 / 100) * 100,
                Type = k % 2 == 0 ? "Refund" : "Reissue",
                Services = t.Services.Select(s => new CanxService
                {
                    S = s,
                    Advance = k % 2 == 1 ? Fmt.Round(s.Payable * .4 / 100) * 100 : 0,
                    Final = Fmt.Round(s.Payable * vp / 100 / 100) * 100,
                }).ToList(),
                CancelDate = t.Start.AddDays(-before[k % 5]),
                Reason = Reasons[k % 5],
            };
        }).ToList();
    }
}
