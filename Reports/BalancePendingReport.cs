using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

/// <summary>A label / value pair of the drill-down "Amount summary" timeline; Color is a CSS colour.</summary>
public sealed record BpLine(string Label, string Value, string Color);

/// <summary>One payment-schedule node: Dot is the inline style of the circle, SubColor the colour of its caption.</summary>
public sealed record BpNode(string Short, string Amount, string Glyph, string Dot, string Date, string Sub, string SubColor);

/// <summary>One receipt (transaction) of a ticket, ready to print.</summary>
public sealed record BpReceipt(string Date, string Exec, string Branch, string Mode, string Detail, string Ref, string Remark, string Amount);

/// <summary>A payment-schedule slab. Off = days from today to its due date; Cum = cumulative amount due.</summary>
public sealed record BpInstalment(int N, int Off, DateTime Date, double Amount, double Cum, string Status)
{
    public string Timing => Off < 0 ? "Date passed" : Off == 0 ? "Due today" : "In " + Off + " days";
}

/// <summary>A received transaction. Off = days from today (zero or negative).</summary>
public sealed record BpTxn(int Off, double Amount, string Mode, string Ref, string Remark, string Detail);

/// <summary>A ticket with money still to collect: totals, payment schedule status and its receipts.</summary>
public sealed class BalanceRow
{
    public required string No { get; init; }
    public required string Mbk { get; init; }
    public string? Prev { get; init; }
    public required string Exec { get; init; }
    public required string Branch { get; init; }
    public required string Name { get; init; }
    public int Pax { get; init; }
    public required string Tour { get; init; }
    public required string Status { get; init; }
    public double Grand { get; init; }
    public double Received { get; init; }
    public DateTime Booked { get; init; }
    public DateTime Start { get; init; }
    public required IReadOnlyList<BpInstalment> Instalments { get; init; }
    public required IReadOnlyList<BpTxn> Txns { get; init; }

    public double Pending => Grand - Received;
    private IEnumerable<BpInstalment> Overdue => Instalments.Where(z => z.Status == "Overdue");
    /// <summary>Days past the first overdue slab date (0 = not overdue).</summary>
    public int OverdueDays => Overdue.Any() ? -Overdue.First().Off : 0;
    public double OverdueAmount => Overdue.Any() ? Math.Max(0, Overdue.Last().Cum - Received) : 0;
    public bool DueThisWeek => Instalments.Any(z => z.Status == "Due");
    public BpInstalment Next => Instalments.First(z => z.Status != "Paid");
    public DateTime? LastPayment => Txns.Count > 0 ? SampleData.Today.AddDays(Txns[^1].Off) : null;
    public bool IsOverdue => OverdueDays > 0;

    // Display-ready values (the view only prints these).
    public string PaxText => Pax + " pax";
    public string PrevText => "Prev " + Prev;
    public string BookedText => BalancePendingReport.Sd(Booked);
    public string StartText => BalancePendingReport.Sd(Start);
    public string GrandText => Fmt.Inr(Grand);
    public string ReceivedText => Fmt.Inr(Received);
    public string PendingText => Fmt.Inr(Pending);
    public string NextDate => BalancePendingReport.Sd(Next.Date);
    public string NextSub => (Next.N == 1 ? "Booking amount" : "Slab " + Next.N) + " · " + Fmt.Inr(Next.Amount);
    public string OverdueText => OverdueDays + (OverdueDays == 1 ? " day" : " days");
    public string? LastText => LastPayment is null ? null : BalancePendingReport.Sd(LastPayment.Value);
    /// <summary>Overdue rows are tinted orange, rows with a previous ticket blue.</summary>
    public string? Tint => IsOverdue ? "background:var(--alert-bg)" : Prev is { Length: > 0 } ? "background:var(--info-bg)" : null;

    /// <summary>Only the ticket's own executive can send a payment link; for everyone else the button is greyed out.</summary>
    public bool CanSend => Exec == BalancePendingReport.CurrentUser;
    public string SendTip => CanSend ? "Send payment link to " + Name : "Only " + Exec + " (the ticket executive) can send payment links";

    public string DrillTitle => "Balance summary — " + No + " · " + Name;
    public string ReceiptCount => Txns.Count + (Txns.Count == 1 ? " receipt" : " receipts");

    public IReadOnlyList<BpLine> Summary
    {
        get
        {
            var l = new List<BpLine>
            {
                new("Total amount", GrandText, "var(--ink-muted)"),
                new("Received", ReceivedText, "var(--ok-ink)"),
                new("Pending balance", PendingText, IsOverdue ? "var(--color-danger)" : "var(--color-action-blue)"),
            };
            if (OverdueAmount > 0) l.Add(new("Overdue", Fmt.Inr(OverdueAmount), "var(--color-danger)"));
            return l;
        }
    }

    public IReadOnlyList<BpNode> Nodes => Instalments.Select(z =>
    {
        var first = z.N == 1;
        var paid = first && Received >= z.Amount;
        var bad = first && !paid && z.Off < 0;
        var bg = paid ? "var(--ok-solid)" : bad ? "var(--alert-bg)" : "#fff";
        var bd = paid ? "var(--ok-solid)" : bad ? "var(--alert-ink)" : first ? "var(--warn-rule)" : "var(--rule)";
        var fg = paid ? "var(--on-ok-solid)" : "var(--alert-ink)";
        var sub = first ? (paid ? "Paid" : z.Off < 0 ? "Overdue" : z.Off <= 7 ? "Due" : "Upcoming") : z.Timing;
        var subFg = paid ? "var(--ok-ink)" : bad ? "var(--alert-ink)" : first ? "var(--warn-ink-deep)" : "var(--ink-muted)";
        return new BpNode(first ? "Booking" : "Slab " + z.N, Fmt.Inr(z.Amount), paid ? "✓" : bad ? "!" : "",
            $"background:{bg};border-color:{bd};color:{fg}", BalancePendingReport.Sd(z.Date), sub, subFg);
    }).ToList();

    public IReadOnlyList<BpReceipt> Receipts => Txns.Select(c => new BpReceipt(
        BalancePendingReport.Sd(SampleData.Today.AddDays(c.Off)), Exec, Branch, c.Mode, c.Detail, c.Ref, c.Remark, Fmt.Inr(c.Amount))).ToList();
}

public sealed class BalancePendingPage : ReportPage
{
    public List<BalanceRow> Rows { get; set; } = new();
    /// <summary>"Showing 1–10 of 10 tickets" / "No tickets".</summary>

    /// <summary>Period options of the prototype; Balance Pending opens on "This Month".</summary>
}

public static class BalancePendingReport
{
    /// <summary>The signed-in user of the prototype; only their own tickets have an enabled "send link" button.</summary>
    public const string CurrentUser = "Anita Rao";

    private static readonly string[] Modes = { "UPI", "Card", "NEFT", "Payment link", "Cash", "Cheque" };
    private static readonly string[] Gateways = { "RazorPay", "PayU", "Instamojo", "Pine Labs" };

    /// <summary>"05 Oct 26" (the prototype's sd()).</summary>
    public static string Sd(DateTime d) => Fmt.Date(d)[..^4] + (d.Year % 100).ToString("D2");

    public static BalancePendingPage Build(ReportQuery q)
    {
        var page = new BalancePendingPage
        {
            Id = "r15",
            Title = "Balance Pending Report",
            Description = "Balance Pending Report — every ticket with money still to collect. Each row shows the total amount, what has been received and the pending balance. Expand a ticket for its payment schedule (30% at booking, 40% at 45 days before the tour, 30% at 15 days before), the receipts so far transaction by transaction, and a button to send the customer a payment link. Only the ticket’s own executive can send a link.",
            Query = q,
            SearchPlaceholder = "Search Ticket No., MBK No., Passenger, Tour, Executive...",
            PeriodField = "Tour Start Date",
            DefaultPeriod = "This Month",
            PagerUnit = "tickets",
            Filters = new FilterDef[]
            {
                new("Branch", new[] { "DEL001", "HYD001", "BLR001", "MAA001" }),
                new("Executive", new[] { "Anita Rao", "Rohit Sen", "Meera Iyer", "Vikram Das", "Karthik Raja" }),
                new("Ticket Status", new[] { "Confirmed", "Pending" }),
                new("Overdue", new[] { "Overdue only", "Not overdue" }),
                new("Due this week", new[] { "Yes", "No" }),
                new("Pending Amount", new[] { "Below ₹25,000", "₹25,000 – ₹1,00,000", "Above ₹1,00,000" }),
            },
            DateFields = new[] { "Tour Start Date" },
        };

        // Default view: Period = This Month, so only tickets whose tour starts in the sample month are listed.
        var today = SampleData.Today;
        var first = new DateTime(today.Year, today.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var f = All()
            .Where(x => x.Pending > 0 && x.Start >= first && x.Start <= last)
            .OrderByDescending(x => x.OverdueDays).ThenBy(x => x.Next.Date)
            .ToList();

        const int LinksToday = 2; // the prototype's seeded send-link log: SPL5622 and GRP4592 today
        page.Kpis.AddRange(new[]
        {
            new Kpi("Total tickets", f.Count.ToString(), Big: true),
            new Kpi("Total amount", Fmt.Inr(f.Sum(x => x.Grand)), Big: true),
            new Kpi("Received", Fmt.Inr(f.Sum(x => x.Received)), "var(--ok-ink)", true),
            new Kpi("Pending balance", Fmt.Inr(f.Sum(x => x.Pending)), "var(--color-action-blue)", true),
            new Kpi("Overdue amount", Fmt.Inr(f.Sum(x => x.OverdueAmount)), "var(--color-danger)", true),
            new Kpi("Due this week", f.Count(x => x.DueThisWeek).ToString(), "var(--alert-ink)", true),
            new Kpi("Links sent today", LinksToday.ToString(), Big: true),
        });
        (page.Rows, page.Pager) = Pager.Paginate(f, q.Page);
        return page;
    }

    private static List<BalanceRow> All()
    {
        var today = SampleData.Today;
        // ticket, executive, branch, customer, pax, tour, start offset (days from today), total, share received, status, previous ticket
        var d = new (string No, string Exec, string Branch, string Name, int Pax, string Tour, int So, long G, double Pct, string Status, string Prev)[]
        {
            ("GRP4590", "Anita Rao", "DEL001", "Rahul Mehta", 4, "Golden Triangle Classic", -3, 148500, .55, "Confirmed", ""),
            ("SPL5622", "Rohit Sen", "HYD001", "Sunita Iyer", 2, "Hyderabad Heritage Weekend", 2, 64200, .3, "Confirmed", ""),
            ("GRP4591", "Anita Rao", "DEL001", "Arjun Kapoor", 6, "Rajasthan Royal Circuit", 4, 214000, .62, "Confirmed", "GRP4102"),
            ("SPL5623", "Meera Iyer", "BLR001", "Deepak Verma", 3, "Coorg Coffee Trails", 8, 96800, .3, "Confirmed", ""),
            ("GRP4592", "Vikram Das", "DEL001", "Ritu Sharma", 5, "Kerala Backwaters", 11, 187300, .45, "Pending", ""),
            ("SPL5624", "Karthik Raja", "MAA001", "Imran Sheikh", 2, "Chettinad Heritage", 14, 58900, .3, "Confirmed", ""),
            ("GRP4593", "Rohit Sen", "HYD001", "Lakshmi Nair", 6, "Deccan Express", 18, 232600, .7, "Confirmed", "GRP4188"),
            ("SPL5625", "Anita Rao", "DEL001", "Vivek Gupta", 4, "Golden Triangle Classic", 21, 132400, .7, "Confirmed", ""),
            ("GRP4594", "Meera Iyer", "BLR001", "Pooja Desai", 2, "Hampi Highlights", 25, 71500, .72, "Pending", ""),
            ("SPL5626", "Vikram Das", "DEL001", "Sandeep Rao", 3, "Rajasthan Royal Circuit", 33, 118700, .72, "Confirmed", ""),
            ("GRP4595", "Karthik Raja", "MAA001", "Anjali Joshi", 7, "South India Grand Tour", 41, 342800, .72, "Confirmed", ""),
            ("SPL5627", "Anita Rao", "DEL001", "Nikhil Bhat", 2, "Delhi City Break", 52, 49800, .3, "Confirmed", ""),
            ("GRP4596", "Rohit Sen", "HYD001", "Shweta Kulkarni", 4, "Hyderabad Heritage Weekend", 60, 126400, .3, "Confirmed", ""),
            ("SPL5628", "Meera Iyer", "BLR001", "Tarun Malhotra", 3, "Coorg Coffee Trails", -1, 84600, .6, "Confirmed", ""),
        };

        var rows = new List<BalanceRow>();
        for (var i = 0; i < d.Length; i++)
        {
            var x = d[i];
            var g = (double)x.G;
            var bo = x.So - (70 + (i % 4) * 6);
            int[] ioff = { bo, x.So - 45, x.So - 15 };
            var a0 = Fmt.Round(g * .3 / 100) * 100;
            var a1 = Fmt.Round(g * .4 / 100) * 100;
            double[] amts = { a0, a1, g - a0 - a1 };
            double[] cum = { amts[0], amts[0] + amts[1], g };
            double recv = Fmt.Round(g * x.Pct / 100) * 100;

            // Receipts fill the slabs in order; a slab-2 payment over ₹20,000 arrives in two parts.
            var txns = new List<BpTxn>();
            var rem = recv;
            for (var k = 0; k < 3; k++)
            {
                var take = Math.Min(rem, amts[k]);
                if (take <= 0) continue;
                rem -= take;
                var parts = k == 1 && take > 20000 ? new[] { Fmt.Round(take * .6 / 100) * 100d, 0d } : new[] { take };
                if (parts.Length > 1) parts[1] = take - parts[0];
                for (var p = 0; p < parts.Length; p++)
                {
                    var mi = (i + k + p) % 6;
                    txns.Add(new BpTxn(Math.Min(0, ioff[k] + p * 3 + (i % 3)), parts[p], Modes[mi],
                        "TXN" + (500000 + (i * 7919 + k * 131 + p * 17) % 400000),
                        k == 0 ? "Booking amount" : "Part payment",
                        Detail(mi, i, k, p, x.Branch)));
                }
            }

            var ins = new List<BpInstalment>();
            for (var k = 0; k < 3; k++)
                ins.Add(new BpInstalment(k + 1, ioff[k], today.AddDays(ioff[k]), amts[k], cum[k],
                    recv >= cum[k] ? "Paid" : ioff[k] < 0 ? "Overdue" : ioff[k] <= 7 ? "Due" : "Upcoming"));

            rows.Add(new BalanceRow
            {
                No = x.No, Mbk = "MBK-" + (88900 + i), Prev = x.Prev.Length > 0 ? x.Prev : null, Exec = x.Exec, Branch = x.Branch,
                Name = x.Name, Pax = x.Pax, Tour = x.Tour, Status = x.Status, Grand = g, Received = recv,
                Booked = today.AddDays(bo), Start = today.AddDays(x.So), Instalments = ins, Txns = txns,
            });
        }
        return rows;
    }

    private static string Detail(int mi, int i, int k, int p, string branch) => mi switch
    {
        0 => "UPI ID " + new[] { "rahul", "priya", "amit" }[(i + k) % 3] + "@okaxis · UPI",
        1 => "Card •••• " + (4000 + (i * 37 + k * 11) % 5999) + " · Auth " + (100000 + (i * 911 + k * 53 + p) % 899999),
        2 => "HDFC ••" + (1000 + (i * 71 + k * 13) % 8999) + " · UTR HDFCN" + (700000 + (i * 313 + k * 19) % 299999),
        3 => Gateways[(i + k) % 4] + " link · Pay ID pay_" + (80000 + (i * 421 + k * 7) % 19999),
        4 => "Cash sheet " + branch + "/" + (5 + (i + k) % 5).ToString("D2") + "10/" + (1 + (i + k) % 2),
        _ => "Cheque " + (308000 + (i * 97 + k * 31) % 999) + " · Bank ••" + (1000 + (i * 17) % 8999) + " · cleared",
    };
}
