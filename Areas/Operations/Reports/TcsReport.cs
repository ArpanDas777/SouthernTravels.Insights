using SouthernTravels.Core.Infrastructure;
using SouthernTravels.Core.Models;

namespace SouthernTravels.Areas.Operations.Reports;

/// <summary>One ticket row of the TCS Report with its display-ready values and hover notes.</summary>
public sealed class TcsRow
{
    public required TaxTicket T { get; init; }

    public bool HasPrev => T.Prev is not null;
    /// <summary>Row tint: orange when TCS on ticket differs from actual, blue when a previous ticket is adjusted.</summary>
    public string Tint => T.Diff != 0 ? "tx-alert" : HasPrev ? "tx-info" : "";
    public string Status => T.Canx is null ? "Confirmed" : "Cancelled";

    public string Booked => "B " + TaxData.Sd(T.Dob);
    public string Journey => "J " + TaxData.Sd(T.Doj);
    public string Completion => "C " + TaxData.Sd(T.Doc);
    public string DiscountText => T.Disc > 0 ? "−" + Fmt.Inr(T.Disc) + " disc." : "₹0 disc.";
    public string GstText => "+" + Fmt.Inr(T.Gst) + " GST";
    public string TicketPct => TaxData.Pct2(T.OnTicket / T.Total * 100);
    public string ActualPct => TaxData.Pct2(T.Actual / T.Total * 100);
    public string DiffText => T.Diff != 0 ? "Diff " + TaxData.Sgn(T.Diff) : "Diff nil";

    public string TicketTip => "On ticket: " + Fmt.Inr(T.OnTicket) + " (" + TicketPct + " of " + Fmt.Inr(T.Total) + ")";
    public string ActualTip => "Actual: " + ActualPct + " of " + Fmt.Inr(T.Total) + " = " + Fmt.Inr(T.Actual);
    public string DiffTip => "Difference = on ticket − actual = " + (T.Diff != 0 ? TaxData.Sgn(T.Diff) : "nil");
    public string FinalTip => T.Prev is { } p
        ? "Current TCS " + Fmt.Inr(T.Actual) + " − Previous TCS " + Fmt.Inr(p.Tcs) + " = " + Fmt.Inr(T.Final)
        : "TCS as per ticket " + Fmt.Inr(T.OnTicket) + " (no previous ticket)";
}

public sealed class TcsPage : ReportPage
{
    public List<TcsRow> Rows { get; set; } = new();
    public TaxFoot? Foot { get; set; }
}

public static class TcsReport
{
    public static TcsPage Build(ReportQuery q)
    {
        var page = new TcsPage
        {
            Id = "r11",
            Title = "TCS Report",
            Description = "TCS Report — Tax Collected at Source on international tour tickets. Shows each ticket’s base amount, discount, GST and grand total, TCS on ticket versus actual TCS at the applicable rate with the difference, any adjustment for a previous (cancelled) ticket, and the final TCS amount. Rows with a previous-ticket adjustment are tinted blue and can be expanded; rows where TCS on ticket differs from actual are tinted orange.",
            Query = q,
            SearchPlaceholder = "Search Ticket No., Passenger, PAN, Executive...",
            PeriodField = "DOB",
            DefaultPeriod = "This Month",
            PagerUnit = "tickets",
            Filters = new FilterDef[]
            {
                new("Month", TaxData.Months),
                new("Executive", TaxData.Executives),
            },
            DateFields = new[] { "DOB" },
        };

        var f = TaxTicket.All.Where(t => TaxData.InPeriod(t.Dob)).OrderByDescending(t => t.Dob).ToList();

        page.Kpis.AddRange(new[]
        {
            new Kpi("Total tickets", f.Count.ToString(), Big: true),
            new Kpi("Base amount", Fmt.Inr(f.Sum(t => t.Base)), Big: true),
            new Kpi("TCS as per ticket", Fmt.Inr(f.Sum(t => t.OnTicket)), "var(--color-action-blue)", true),
            new Kpi("Final TCS amount", Fmt.Inr(f.Sum(t => t.Final)), "var(--ok-ink)", true),
        });

        if (f.Count > 0)
            page.Foot = new TaxFoot("Totals", new[]
            {
                ("Base amount", Fmt.Inr(f.Sum(t => t.Base))),
                ("Grand total", Fmt.Inr(f.Sum(t => t.Grand))),
                ("TCS as per ticket", Fmt.Inr(f.Sum(t => t.OnTicket))),
                ("Previous TCS adjusted", Fmt.Inr(f.Sum(t => t.Prev?.Tcs ?? 0))),
            }, "TCS liability", Fmt.Inr(f.Sum(t => t.Final)));

        var rows = f.Select(t => new TcsRow { T = t }).ToList();
        (page.Rows, page.Pager) = Pager.Paginate(rows, q.Page);
        return page;
    }
}
