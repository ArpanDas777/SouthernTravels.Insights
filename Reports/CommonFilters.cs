using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Reports;

/// <summary>Filter sets and matching rules shared by several reports.</summary>
public static class CommonFilters
{
    public static readonly string[] ServiceTypes = { "Accommodation", "Transport", "Guide", "En-Route Meals", "Other" };
    public static readonly string[] Cities = { "Agra", "Delhi", "Hyderabad", "Jaipur" };
    public static readonly string[] Execs = { "Anita Rao", "Rohit Sen", "Vikram Das" };
    public static readonly string[] Branches = { "Delhi", "Hyderabad" };
    public static readonly string[] OpsUsers = { "Priya Nair", "Karan Shah" };

    /// <summary>The ten dropdowns used by Ticket &amp; Service Status and Service wise Report.</summary>
    public static readonly IReadOnlyList<FilterDef> TicketService = new FilterDef[]
    {
        new("Ticket Status", new[] { "Confirmed", "Cancelled" }),
        new("OPS Status", new[] { "New", "In Progress", "Completed" }),
        new("Booking Executive", Execs),
        new("Booking Branch", Branches),
        new("OPS User", OpsUsers),
        new("OPS Branch", new[] { "Delhi (DEL001)", "Hyderabad (HYD001)" }),
        new("Service Type", ServiceTypes),
        new("Service City", Cities),
        new("Service Status", new[] { "Confirmed", "Pending", "Cancelled" }),
        new("Vendor Name", SampleData.Vendors.Values.Select(v => v.Name).ToArray()),
    };

    public static readonly string[] TicketServiceDates = { "Booking Date", "Tour Start Date", "Service Start Date", "Confirmation Date" };

    /// <summary>Ticket-level checks: Ticket Status, Booking Executive, Booking Branch, Booking Date, Tour Start Date.</summary>
    public static bool TicketLevel(Ticket t, ReportQuery q) =>
        ReportQuery.Match(q.F("Ticket Status"), t.Status)
        && ReportQuery.Match(q.F("Booking Executive"), t.Exec)
        && ReportQuery.Match(q.F("Booking Branch"), t.Branch)
        && q.InRange("Booking Date", t.Booked)
        && q.InRange("Tour Start Date", t.Start);

    /// <summary>Service-level checks: OPS Status/User/Branch, Service Type/City/Status, Vendor, Service Start, Confirmation Date.</summary>
    public static bool ServiceLevel(Service s, ReportQuery q)
    {
        var opsBranch = q.F("OPS Branch");
        return ReportQuery.Match(q.F("OPS Status"), s.OpsDisplay)
            && ReportQuery.Match(q.F("OPS User"), s.Ops)
            && (opsBranch is null || opsBranch.Contains(s.OpsBranch))
            && ReportQuery.Match(q.F("Service Type"), s.Type)
            && ReportQuery.Match(q.F("Service City"), s.City)
            && ReportQuery.Match(q.F("Service Status"), s.Status)
            && ReportQuery.Match(q.F("Vendor Name"), SampleData.VendorName(s.VendorId))
            && q.InRange("Service Start Date", s.Start)
            && q.InRange("Confirmation Date", s.Conf);
    }
}

/// <summary>A service paired with its ticket and derived pricing (the prototype's "SV" rows).</summary>
public sealed class ServiceRow
{
    public required Ticket T { get; init; }
    public required Service S { get; init; }
    public int Si { get; init; }
    public string Id => T.Index + "-" + Si;
    public string VendorName => SampleData.VendorName(S.VendorId);
    public double Gst => Fmt.Round(T.Grand * 5 / 105);
    public double Discount => T.Index % 3 == 1 ? 2000 : 0;
    /// <summary>Base price + margin.</summary>
    public double BaseMargin => T.Grand - Gst + Discount;
    public double Negotiated => S.Payable;
    public double Contracted => Fmt.Round(S.Payable * 1.07 / 100) * 100;
    public double Variation => Contracted - Negotiated;
    public string Rooms => S.Type == "Accommodation" ? (T.Index % 3 + 1).ToString() : "-";

    public static List<ServiceRow> All() =>
        SampleData.Tickets.SelectMany(t => t.Services.Select((s, si) => new ServiceRow { T = t, S = s, Si = si })).ToList();

    /// <summary>Pax counted once per ticket.</summary>
    public static int PaxOf(IEnumerable<ServiceRow> rows) => rows.GroupBy(x => x.T.No).Sum(g => g.First().T.Pax);
}
