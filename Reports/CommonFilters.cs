using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Reports;

/// <summary>Dropdown options for the (visual-only) filter panels.</summary>
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

    /// <summary>
    /// The same ten dropdowns filled from the Operations API (Reports/GetOperationsReportFilters); each option's value is the API's id or code.
    /// Falls back to the built-in list for any list the API did not return, and to <see cref="TicketService"/> when there is no API data.
    /// Service Type is not in the API, so it keeps the fixed list.
    /// </summary>
    public static IReadOnlyList<FilterDef> TicketServiceFrom(OperationsReportFilters? d)
    {
        if (d is null) return TicketService;
        var branchNames = d.BookingBranch.Where(b => !string.IsNullOrWhiteSpace(b.BranchCode))
            .GroupBy(b => b.BranchCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().BranchName?.Trim() ?? "", StringComparer.OrdinalIgnoreCase);
        string BranchText(string code) => branchNames.TryGetValue(code, out var n) && n.Length > 0 ? $"{n} ({code})" : code;

        return new FilterDef[]
        {
            Def("Ticket Status", d.TicketStatus.Select(x => ((string?)x.StatusID.ToString(), x.StatusName)), TicketService[0], sort: false),
            Def("OPS Status", d.OpsStatus.Select(x => ((string?)x.StatusID.ToString(), x.StatusName)), TicketService[1], sort: false),
            Def("Booking Executive", d.BookingExecutive.Select(x => (x.UserName, x.UserName)), TicketService[2]),
            Def("Booking Branch", d.BookingBranch.Select(x => (x.BranchCode, x.BranchCode is { } c ? BranchText(c.Trim()) : null)), TicketService[3]),
            Def("OPS User", d.OpsUser.Select(x => (x.ExecutivesId, x.Name)), TicketService[4]),
            Def("OPS Branch", d.OpsBranch.Select(x => (x.OpsBranch, x.OpsBranch is { } c ? BranchText(c.Trim()) : null)), TicketService[5]),
            TicketService[6], // Service Type
            Def("Service City", d.ServiceCity.Select(x => ((string?)x.CityID.ToString(), x.CityName)), TicketService[7]),
            Def("Service Status", d.ServiceStatus.Select(x => ((string?)x.StatusID.ToString(), x.StatusName)), TicketService[8], sort: false),
            Def("Vendor Name", d.Vendor.Select(x => ((string?)x.HotelID.ToString(), x.HotelName)), TicketService[9]),
        };
    }

    /// <summary>Trims, drops blanks and duplicate values, sorts by text (except the short status lists, which keep the API's order).</summary>
    static FilterDef Def(string label, IEnumerable<(string? Value, string? Text)> items, FilterDef fallback, bool sort = true)
    {
        var options = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Value) && !string.IsNullOrWhiteSpace(i.Text))
            .Select(i => new FilterOption(i.Value!.Trim(), i.Text!.Trim()))
            .DistinctBy(o => o.Value, StringComparer.OrdinalIgnoreCase);
        if (sort) options = options.OrderBy(o => o.Text, StringComparer.OrdinalIgnoreCase);
        var list = options.ToList();
        return list.Count > 0 ? new FilterDef(label, list) : fallback;
    }

    public static readonly string[] TicketServiceDates = { "Booking Date", "Tour Start Date", "Service Start Date", "Confirmation Date" };
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
