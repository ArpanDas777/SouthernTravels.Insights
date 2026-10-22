using SouthernTravels.Areas.Operations.Data;
using SouthernTravels.Core.Infrastructure;
using SouthernTravels.Core.Models;

namespace SouthernTravels.Areas.Operations.Reports;

/// <summary>One generated / not-generated paperwork cell: the badge, who generated it and when.</summary>
public sealed record SlipCell(bool Done, string? Date, string? By, string Tip);

/// <summary>A CAB ticket with the status of its Initial Duty Slip, Final Duty Slip and Final Bill.</summary>
public sealed class DutySlipRow
{
    public required string No { get; init; }
    public required string Exec { get; init; }
    public required string Branch { get; init; }
    public required string Name { get; init; }
    public int Pax { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public DateTime? Initial { get; init; }
    public DateTime? Final { get; init; }
    public DateTime? Bill { get; init; }
    public required string InitialBy { get; init; }
    public required string FinalBy { get; init; }
    public required string BillBy { get; init; }

    /// <summary>Whole days from today to the journey (negative once departed).</summary>
    public int DaysLeft => (Start - SampleData.Today).Days;

    public string StartText => Fmt.Date(Start);
    public string EndText => "to " + Fmt.Date(End);
    public string PaxText => Pax + " pax";

    /// <summary>Departed / Departs today / "N days left" with its chip class (the prototype's "dlc").</summary>
    public string DaysLeftText => DaysLeft < 0 ? "Departed" : DaysLeft == 0 ? "Departs today" : DaysLeft + (DaysLeft == 1 ? " day left" : " days left");
    public string DaysLeftCls => DaysLeft < 0 ? "c-mut fw6" : "ds-dl " + (DaysLeft <= 7 ? "soon" : "far");

    /// <summary>Departed (or departing today) with the final bill still pending: tinted row.</summary>
    public string? Tint => DaysLeft <= 0 && Bill is null ? "background:var(--alert-bg)" : null;

    public SlipCell InitialSlip => Slip(Initial, InitialBy, "Initial duty slip");
    public SlipCell FinalSlip => Slip(Final, FinalBy, "Final duty slip");
    public SlipCell FinalBill => Slip(Bill, BillBy, "Final bill");

    private static SlipCell Slip(DateTime? d, string by, string name) =>
        new(d is not null, d is null ? null : Fmt.Date(d.Value), by, "Click to view " + name.ToLowerInvariant());
}

public sealed class DutySlipPage : ReportPage
{
    public List<DutySlipRow> Rows { get; set; } = new();
    /// <summary>"Showing 1–10 of 12 tickets" / "No tickets".</summary>
}

public static class DutySlipReport
{
    public static DutySlipPage Build(ReportQuery q)
    {
        var page = new DutySlipPage
        {
            Id = "r10",
            Title = "Duty Slip Report",
            Description = "Duty Slip Report — paperwork status of every CAB ticket. Shows the booking executive and branch, customer and pax, journey dates, and whether the Initial Duty Slip, Final Duty Slip and Final Bill have been generated (with the date each was generated). Use it to chase CAB tickets whose slips or bill are still pending.",
            Query = q,
            SearchPlaceholder = "Search Ticket No., Customer, Booking Executive...",
            PeriodField = "Journey Date",
            DefaultPeriod = "This Month",
            PagerUnit = "tickets",
            HasExpand = false,
            Filters = new FilterDef[]
            {
                new("Branch", new[] { "DEL001", "HYD001", "BLR001", "MAA001" }),
                new("Booking Executive", new[] { "Anita Rao", "Rohit Sen", "Vikram Das", "Meera Iyer", "Karthik Raja" }),
                new("Initial Slip Status", new[] { "Generated", "Not generated" }),
                new("Final Slip Status", new[] { "Generated", "Not generated" }),
                new("Final Bill Status", new[] { "Generated", "Not generated" }),
            },
            DateFields = new[] { "Journey Date" },
        };

        var f = Build();

        page.Kpis.AddRange(new[]
        {
            new Kpi("Total CAB Tickets", f.Count.ToString(), Big: true),
            new Kpi("Initial Slip Pending", f.Count(x => x.Initial is null).ToString(), "var(--alert-ink)", true),
            new Kpi("Final Slip Pending", f.Count(x => x.Final is null).ToString(), "var(--alert-ink)", true),
            new Kpi("Final Bill Pending", f.Count(x => x.Bill is null).ToString(), "var(--alert-ink)", true),
            new Kpi("Fully Complete", f.Count(x => x.Bill is not null).ToString(), "var(--ok-ink)", true),
        });
        (page.Rows, page.Pager) = Pager.Paginate(f, q.Page);
        return page;
    }

    private static List<DutySlipRow> Build()
    {
        var b = new DateTime(2026, 9, 30);
        // ticket, executive, branch, customer, pax, start offset (days after 30 Sep), trip days, paperwork level (0-3)
        var d = new (int No, string Exec, string Branch, string Name, int Pax, int Off, int Days, int Lv)[]
        {
            (641, "Anita Rao", "DEL001", "Rahul Mehta", 4, 1, 3, 3), (642, "Rohit Sen", "HYD001", "Sunita Rao", 2, 2, 2, 3),
            (643, "Meera Iyer", "BLR001", "Deepak Verma", 3, 2, 2, 2), (644, "Vikram Das", "DEL001", "Ritu Sharma", 5, 4, 1, 1),
            (645, "Karthik Raja", "MAA001", "Imran Sheikh", 2, 7, 2, 1), (646, "Anita Rao", "DEL001", "Lakshmi Nair", 6, 9, 3, 1),
            (647, "Rohit Sen", "HYD001", "Vivek Gupta", 4, 12, 2, 0), (648, "Meera Iyer", "BLR001", "Pooja Desai", 2, 15, 1, 1),
            (649, "Vikram Das", "DEL001", "Sandeep Rao", 3, 18, 5, 0), (650, "Karthik Raja", "MAA001", "Anjali Joshi", 7, 21, 2, 0),
            (651, "Anita Rao", "DEL001", "Nikhil Bhat", 2, 24, 3, 1), (652, "Rohit Sen", "HYD001", "Shweta Kulkarni", 4, 27, 2, 0),
        };
        string[][] staff = { new[] { "Priya Nair", "DEL001" }, new[] { "Karan Shah", "HYD001" }, new[] { "Sowmya Rao", "BLR001" }, new[] { "Divya Menon", "MAA001" } };
        static string Who(string[] p) => p[0] + " · " + p[1];

        var list = new List<DutySlipRow>();
        for (var i = 0; i < d.Length; i++)
        {
            var x = d[i];
            var start = b.AddDays(x.Off);
            var end = b.AddDays(x.Off + x.Days - 1);
            list.Add(new DutySlipRow
            {
                No = "CAB" + (12000 + x.No), Exec = x.Exec, Branch = x.Branch, Name = x.Name, Pax = x.Pax, Start = start, End = end,
                Initial = x.Lv >= 1 ? b.AddDays(Math.Min(x.Off - 2, 4)) : null,
                Final = x.Lv >= 2 ? end.AddDays(1) : null,
                Bill = x.Lv >= 3 ? end.AddDays(2) : null,
                InitialBy = x.Exec + " · " + x.Branch,
                FinalBy = Who(staff[i % 4]),
                BillBy = Who(staff[(i + 1) % 4]),
            });
        }
        return list.OrderByDescending(r => r.Start).ToList();
    }
}
