using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

/// <summary>An advance requested by OPS against one vendor service, with its L1 / L2 approval chain.</summary>
public sealed class AdvanceRequest
{
    public required ServiceRow X { get; init; }
    public int K { get; init; }
    /// <summary>L1 (Finance) status: Approved / Requested / Rejected.</summary>
    public required string L1 { get; init; }
    /// <summary>L2 (Management) status: Approved / Requested / "-" (not reached).</summary>
    public required string L2 { get; init; }
    public string Overall => L1 == "Rejected" ? "Rejected" : L1 == "Approved" && L2 == "Approved" ? "Approved" : "Requested";
    /// <summary>Paid / Pending / "-".</summary>
    public string Payment => Overall == "Approved" ? (K % 8 == 0 ? "Paid" : "Pending") : "-";
    public double Amount => Fmt.Round(X.S.Payable * .4 / 100) * 100;
    public double Previous => K % 2 == 1 ? Fmt.Round(X.S.Payable * .2 / 100) * 100 : 0;
    public double Balance => X.S.Payable - Previous - (Payment == "Paid" ? Amount : 0);
    public double Advanced => Previous + (Payment == "Paid" ? Amount : 0);
    public DateTime RequestedDate => X.T.Booked.AddDays(5 + K);
    public string RequestedOn => Fmt.Date(RequestedDate) + ", 10:" + (10 + K * 3).ToString("D2");
    public DateTime L1Date => X.T.Booked.AddDays(6 + K);
    public string L1On => Fmt.Date(L1Date) + ", 14:05";
    public string L2By => K % 2 == 1 ? "Sowmya" : "Divya";
    public DateTime L2Date => X.T.Booked.AddDays(7 + K);
    public string L2On => Fmt.Date(L2Date) + ", 16:40";
    public string Due => Fmt.Date(X.S.Start.AddDays(-3));
    public string PaidOn => Fmt.Date(X.T.Booked.AddDays(9 + K));
}

public sealed class AdvancePage : ReportPage
{
    public List<AdvanceRequest> Rows { get; set; } = new();
}

public static class AdvanceReport
{
    public static AdvancePage Build(ReportQuery q)
    {
        var page = new AdvancePage
        {
            Id = "r6",
            Title = "Advance Report",
            Description = "Advance Report — service-wise advance payment tracking. Shows all advance requests raised by OPS against vendor services, the approval chain, and payment status. Only services where an advance has been formally requested appear here.",
            Query = q,
            SearchPlaceholder = "Search MBK, Ticket No., Passenger, Vendor, Service...",
            PeriodField = "Advance Requested Date",
            Filters = new FilterDef[]
            {
                new("Approval Status", new[] { "Requested", "Approved", "Rejected" }),
                new("Adv. Payment Status", new[] { "Pending", "Paid" }),
                new("OPS User", CommonFilters.OpsUsers),
                new("Service Type", CommonFilters.ServiceTypes),
            },
            DateFields = new[] { "Tour Start Date" },
        };

        var f = Build()
            .Where(x => q.OkPeriod(x.RequestedDate) && q.OkSearch(x.X.T.Mbk + " " + x.X.T.No + " " + x.X.T.PaxName + " " + x.X.VendorName + " " + x.X.S.Name))
            .Where(x => ReportQuery.Match(q.F("Approval Status"), x.Overall)
                        && ReportQuery.Match(q.F("Adv. Payment Status"), x.Payment)
                        && ReportQuery.Match(q.F("OPS User"), x.X.S.Ops)
                        && ReportQuery.Match(q.F("Service Type"), x.X.S.Type)
                        && q.InRange("Tour Start Date", x.X.T.Start))
            .ToList();

        page.Kpis.AddRange(new[]
        {
            new Kpi("Total Requests", f.Count.ToString(), Big: true),
            new Kpi("Approved", f.Count(x => x.Overall == "Approved").ToString(), "var(--ok-ink)", true),
            new Kpi("Pending Approval", f.Count(x => x.Overall == "Requested").ToString(), "var(--color-action-blue)", true),
            new Kpi("Rejected", f.Count(x => x.Overall == "Rejected").ToString(), "var(--color-danger)", true),
            new Kpi("Total Advance Paid", Fmt.Inr(f.Where(x => x.Payment == "Paid").Sum(x => x.Amount)), "var(--ok-ink)"),
            new Kpi("Pending Approval Amt.", Fmt.Inr(f.Where(x => x.Overall == "Requested").Sum(x => x.Amount)), "var(--color-action-blue)"),
            new Kpi("Outstanding Balance", Fmt.Inr(f.Where(x => x.Overall == "Approved").Sum(x => x.Balance)), "var(--color-danger)"),
        });
        page.ResultLabel = f.Count + " requests";
        (page.Rows, page.Pager) = Pager.Paginate(f, q.Page);
        return page;
    }

    private static List<AdvanceRequest> Build()
    {
        var tickets = SampleData.Tickets;
        var list = new List<AdvanceRequest>();
        int[] picks = { 0, 1, 2, 5, 6, 7, 9, 12, 13, 15 };
        for (var k = 0; k < picks.Length; k++)
        {
            if (picks[k] >= tickets.Count) continue;
            var t = tickets[picks[k]];
            if (t.Status == "Cancelled") continue;
            var si = k % t.Services.Count;
            var l1 = k % 4 == 2 ? "Requested" : k % 4 == 3 ? "Rejected" : "Approved";
            var l2 = l1 != "Approved" ? "-" : k % 4 == 0 ? "Approved" : k % 4 == 1 ? "Requested" : "-";
            list.Add(new AdvanceRequest { X = new ServiceRow { T = t, S = t.Services[si], Si = si }, K = k, L1 = l1, L2 = l2 });
        }
        return list;
    }
}
