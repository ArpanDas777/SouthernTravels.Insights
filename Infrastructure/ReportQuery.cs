using Microsoft.AspNetCore.Http;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Infrastructure;

/// <summary>
/// The only state a report page reads from the URL: the active tab, the page number and the
/// Ticket &amp; Service Status "Show all" link. Search, filters, period and the other controls are visual only.
/// </summary>
public sealed class ReportQuery
{
    public string? Tab { get; private init; }
    public int Page { get; private init; } = 1;
    public bool ShowAll { get; private init; }
    /// <summary>Trial Balance vendor id (a link from the vendor list).</summary>
    public string? Vendor { get; private init; }

    public static ReportQuery From(IQueryCollection q) => new()
    {
        Tab = q["tab"].ToString() is { Length: > 0 } tab ? tab : null,
        Page = int.TryParse(q["page"], out var p) && p > 0 ? p : 1,
        ShowAll = q["all"] == "1",
        Vendor = q["vendor"].ToString() is { Length: > 0 } vendor ? vendor : null,
    };
}
