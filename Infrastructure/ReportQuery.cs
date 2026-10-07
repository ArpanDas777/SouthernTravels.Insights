using System.Text;
using Microsoft.AspNetCore.Http;
using SouthernTravels.Insights.Data;

namespace SouthernTravels.Insights.Infrastructure;

/// <summary>
/// Report state carried in the query string. Query keys:
/// tab, page, q (search), period, pf/pt (custom period), all (show all files),
/// f_{slug} (dropdown filters), r_{slug}_from / r_{slug}_to (date-range filters),
/// wf/wt (OPS Workload range), wk (collapsed calendar weeks), bc (collapsed branches).
/// </summary>
public sealed class ReportQuery
{
    public static readonly string[] PeriodOptions = { "All time", "Today", "This Week", "This Month", "This Quarter", "This Year", "Custom" };

    public string? Tab { get; private init; }
    public int Page { get; private init; } = 1;
    public string Search { get; private init; } = "";
    public string Period { get; private init; } = "All time";
    public string PFrom { get; private init; } = "";
    public string PTo { get; private init; } = "";
    public bool ShowAll { get; private init; }
    public IReadOnlyDictionary<string, string> Filters { get; private init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, (DateTime? From, DateTime? To)> Ranges { get; private init; } = new Dictionary<string, (DateTime?, DateTime?)>();
    public string WFrom { get; private init; } = "2026-09-01";
    public string WTo { get; private init; } = "2026-09-30";
    public IReadOnlySet<int> CollapsedWeeks { get; private init; } = new HashSet<int>();
    public IReadOnlySet<string> CollapsedBranches { get; private init; } = new HashSet<string>();

    public string SearchLower => Search.Trim().ToLowerInvariant();

    public static ReportQuery From(IQueryCollection q)
    {
        var filters = new Dictionary<string, string>();
        var froms = new Dictionary<string, DateTime?>();
        var tos = new Dictionary<string, DateTime?>();
        foreach (var key in q.Keys)
        {
            var v = q[key].ToString();
            if (key.StartsWith("f_") && v.Length > 0 && v != "All") filters[key[2..]] = v;
            else if (key.StartsWith("r_") && key.EndsWith("_from")) froms[key[2..^5]] = Fmt.ParseIso(v);
            else if (key.StartsWith("r_") && key.EndsWith("_to")) tos[key[2..^3]] = Fmt.ParseIso(v);
        }
        var ranges = froms.Keys.Union(tos.Keys).ToDictionary(k => k, k => (froms.GetValueOrDefault(k), tos.GetValueOrDefault(k)));
        var period = q["period"].ToString();
        return new ReportQuery
        {
            Tab = q["tab"].ToString() is { Length: > 0 } tab ? tab : null,
            Page = int.TryParse(q["page"], out var p) && p > 0 ? p : 1,
            Search = q["q"].ToString(),
            Period = PeriodOptions.Contains(period) ? period : "All time",
            PFrom = q["pf"].ToString(),
            PTo = q["pt"].ToString(),
            ShowAll = q["all"] == "1",
            Filters = filters,
            Ranges = ranges,
            WFrom = q.ContainsKey("wf") ? q["wf"].ToString() : "2026-09-01",
            WTo = q.ContainsKey("wt") ? q["wt"].ToString() : "2026-09-30",
            CollapsedWeeks = q["wk"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out var n) ? n : -1).Where(n => n >= 0).ToHashSet(),
            CollapsedBranches = q["bc"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(),
        };
    }

    /// <summary>"Ticket Status" → "ticketStatus".</summary>
    public static string Slug(string label)
    {
        var words = new string(label.Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var w in words)
        {
            var lower = w.ToLowerInvariant();
            sb.Append(sb.Length == 0 ? lower : char.ToUpperInvariant(lower[0]) + lower[1..]);
        }
        return sb.ToString();
    }

    /// <summary>Selected dropdown value for a filter label, or null for "All".</summary>
    public string? F(string label) => Filters.TryGetValue(Slug(label), out var v) ? v : null;

    public static bool Match(string? selected, string value) => selected is null || selected == value;

    public (DateTime? From, DateTime? To) Range(string label) =>
        Ranges.TryGetValue(Slug(label), out var r) ? r : (null, null);

    /// <summary>Date-range filter; only applies once both ends are chosen.</summary>
    public bool InRange(string label, DateTime? v)
    {
        var (from, to) = Range(label);
        if (from is null || to is null) return true;
        return v is not null && v >= from && v <= to;
    }

    /// <summary>Overlap filter: the span [start, end] overlaps the chosen range.</summary>
    public bool Overlaps(string label, DateTime start, DateTime end)
    {
        var (from, to) = Range(label);
        if (from is null || to is null) return true;
        return start <= to && end >= from;
    }

    public int ActiveFilterCount => Filters.Count + Ranges.Count(r => r.Value.From is not null && r.Value.To is not null);

    /// <summary>Search, any dropdown, a date range or the Period is active ("Clear all" shows).</summary>
    public bool AnyActive => SearchLower.Length > 0 || ActiveFilterCount > 0 || Period != "All time"
                             || Ranges.Any(r => r.Value.From is not null);

    public bool OkSearch(string haystack) => SearchLower.Length == 0 || haystack.ToLowerInvariant().Contains(SearchLower);

    /// <summary>Shared Period control range (null when "All time").</summary>
    public (DateTime From, DateTime To)? PeriodRange
    {
        get
        {
            var d = SampleData.Today;
            var y = d.Year; var m = d.Month;
            var wk = d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
            var qStart = new DateTime(y, (m - 1) / 3 * 3 + 1, 1);
            return Period switch
            {
                "Today" => (d, d),
                "This Week" => (wk, wk.AddDays(6)),
                "This Month" => (new DateTime(y, m, 1), new DateTime(y, m, 1).AddMonths(1).AddDays(-1)),
                "This Quarter" => (qStart, qStart.AddMonths(3).AddDays(-1)),
                "This Year" => (new DateTime(y, 1, 1), new DateTime(y, 12, 31)),
                "Custom" => (Fmt.ParseIso(PFrom) ?? DateTime.MinValue, Fmt.ParseIso(PTo) ?? DateTime.MaxValue.Date),
                _ => null,
            };
        }
    }

    public bool OkPeriod(DateTime v)
    {
        if (PeriodRange is not { } pr) return true;
        return v >= pr.From && (pr.To == DateTime.MaxValue.Date || v < pr.To.AddDays(1));
    }

    public string PeriodNote(string field)
    {
        if (PeriodRange is not { } pr) return "";
        var span = Period == "Custom"
            ? (PFrom.Length > 0 ? PFrom : "…") + " → " + (PTo.Length > 0 ? PTo : "…")
            : Fmt.Date(pr.From) + " → " + Fmt.Date(pr.To);
        return "Filtering by " + field + " " + span;
    }
}
