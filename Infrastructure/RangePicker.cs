using System.Globalization;

namespace SouthernTravels.Insights.Infrastructure;

/// <summary>
/// Display model of the single date-range control: one button that opens a calendar with the range tinted
/// (start / end marked). Visual only; the calendar shows the month the range starts in.
/// </summary>
public sealed record RangePicker(string Button, string Month, int LeadBlanks, IReadOnlyList<(string Text, string Cls)> Days, string Footer)
{
    public static RangePicker For(DateTime from, DateTime to)
    {
        var first = new DateTime(from.Year, from.Month, 1);
        var days = Enumerable.Range(1, DateTime.DaysInMonth(first.Year, first.Month)).Select(d =>
        {
            var date = first.AddDays(d - 1);
            return (d.ToString(CultureInfo.InvariantCulture), date == from || date == to ? "end" : date > from && date < to ? "mid" : "");
        }).ToList();
        return new RangePicker(Fmt.Date(from) + " – " + Fmt.Date(to), first.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            (int)first.DayOfWeek, days, "Start " + Fmt.Date(from) + " · End " + Fmt.Date(to));
    }
}
