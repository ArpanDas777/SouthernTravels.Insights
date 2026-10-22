using SouthernTravels.Core.Infrastructure;
using SouthernTravels.Core.Models;

namespace SouthernTravels.Areas.Operations.Reports;

/// <summary>An amount cell: display text, colour / weight classes and a native tooltip (what the prototype opened as a modal).</summary>
public sealed record CsAmt(string Text, string Cls, string? Tip);

/// <summary>One cash sheet in the Cash Sheet View grid. Amts = 12 payment modes, then payment against booking, reissue, refund, cancellation charges and cash.</summary>
public sealed record CsGridRow(string Date, string Branch, string Cashier, string Shift, string Serial, IReadOnlyList<CsAmt> Amts, string Total);

/// <summary>A line inside a card: a group heading or a label / value pair.</summary>
public sealed record CsKv(string Label, string Value, string Cls = "", bool Group = false);

/// <summary>Status pill text and tone (ok / pend).</summary>
public sealed record CsStatus(string Text, string Tone);

/// <summary>One cash sheet in the Rows view.</summary>
public sealed record CsRowsItem(string Date, string Serial, string Who, IReadOnlyList<CsKv> Collections, IReadOnlyList<CsKv> Adjustments,
    string Cash, string Total, string Deposit, string DepositDate, string Bank, string? Slip, bool Approve,
    string CashIn, string Opening, string Closing, string Entry, string Remarks, bool HasRemarks, CsStatus Status);

public sealed record CsDetailSection(string Title, IReadOnlyList<CsKv> Items);

/// <summary>One cash sheet in the Detail view: its list entry and its detail cards.</summary>
public sealed record CsDetailItem(string Date, string Serial, string Total, string Who, CsStatus Status,
    string Heading, string HeadingWho, IReadOnlyList<CsDetailSection> Sections);

/// <summary>One cash ledger line (a collection or a deposit) with the running balance.</summary>
public sealed record CsLedgerEntry(string Date, string Text, string Sub, bool Deposit, string In, string Out, string Balance, CsStatus Status, bool Approve);

/// <summary>A branch in the Cash Ledger: the rail entry and its ledger.</summary>
public sealed record CsLedgerBranch(string Branch, string Closing, string PendingText, bool HasPending, string Opening, string In, string Out,
    string Range, string OpenDate, IReadOnlyList<CsLedgerEntry> Entries);

public sealed class CashSheetAuditPage : ReportPage
{
    /// <summary>day = Cash Sheet View grid, rows, detail, ledger (the `tab` query value).</summary>
    public string View { get; init; } = "day";
    public bool IsLedger => View == "ledger";
    public IReadOnlyList<CsKv> ViewOptions { get; init; } = Array.Empty<CsKv>();

    public static readonly string[] PeriodChoices = { "All time", "Today", "Last 5 Days", "This Week", "This Month", "This Quarter", "This Year", "Custom" };
    public string PeriodSelected => "Last 5 Days";
    public string LedgerFrom => "2026-10-01";
    public string LedgerTo => "2026-10-05";

    public static readonly string[] GridHeaders =
    {
        "Date", "Cashier Branch", "Cashier Name", "Shift", "Cash Sheet Serial No.",
        "CBI DELHI 9432 (via Bank Deposit)", "CBI DELHI 4362 (via Bank Deposit)", "SBI HYD 5964 (via Bank Deposit)", "Pine Lab QR Code (via Bank Deposit)",
        "Pine Labs POS", "Instamojo", "RazorPay", "PayU", "Pine Lab", "Instamojo", "RazorPay", "PayU",
        "Payment Against Booking", "Reissue Deposits", "Refund", "Cancellation Charges", "Cash", "Total Sales (excl. cancellation income)",
    };

    /// <summary>Group heading over the grid columns: (colspan, label).</summary>
    public static readonly (int Span, string Label)[] GridGroups =
    {
        (1, "Cash sheet"), (1, ""), (3, ""), (4, "Bank deposit modes"), (1, "POS"), (4, "Payment links (Multipurpose GRP, w/o GST)"),
        (3, "Online – balance clearance & website"), (4, "Booking adjustments"), (2, "Cash & sales"),
    };

    public List<CsGridRow> GridRows { get; set; } = new();
    public List<CsRowsItem> RowsItems { get; set; } = new();
    public List<CsDetailItem> DetailItems { get; set; } = new();
    public List<CsLedgerBranch> Branches { get; set; } = new();
}

public static class CashSheetAuditReport
{
    private static readonly TabDef[] Tabs = { new("day", "Cash Sheet View"), new("ledger", "Cash Ledger") };
    private static readonly string[] Views = { "day", "rows", "detail", "ledger" };

    private static readonly string[] Mn = { "CBI DELHI 9432", "CBI DELHI 4362", "SBI HYD 5964", "Pine Lab QR", "Pine Labs POS", "Instamojo", "RazorPay", "PayU", "Pine Lab", "Instamojo", "RazorPay", "PayU" };
    private static readonly string[] ModeGroup = { "Bank deposit", "Bank deposit", "Bank deposit", "Bank deposit", "POS", "Payment links", "Payment links", "Payment links", "Payment links", "Online", "Online", "Online" };
    private static readonly string[] Branches = { "DEL001", "HYD001", "BLR001", "MAA001" };
    private static readonly DateTime Day1 = new(2026, 10, 1);

    private const string Navy = "var(--color-navy)", Blue = "var(--color-action-blue)", Red = "var(--color-danger)", Amb = "var(--alert-ink)", Grn = "var(--ok-ink)";

    private sealed class Sheet
    {
        public required int Day { get; init; }
        public required string Branch { get; init; }
        public required string Cashier { get; init; }
        public required int Shift { get; init; }
        public required double[] M { get; init; }
        public double Pab { get; init; }
        public double Rei { get; init; }
        public double Ref { get; init; }
        public double Canx { get; init; }
        public double Cash { get; init; }
        public double Open { get; init; }
        public int DepDay { get; init; }
        public double Dep { get; init; }
        public string Bank { get; init; } = "";
        public string Rem { get; init; } = "";
        public required string Status { get; init; }

        public DateTime Date => Day1.AddDays(Day - 1);
        public DateTime DepDate => Day1.AddDays(DepDay - 1);
        public string Serial => Branch + "/" + Day.ToString("D2") + "10/" + Shift;
        public double Tot => M.Sum() + Pab + Rei + Ref + Cash;
        public double Close => Open + Cash - Dep;
        public string Who => Branch + " · " + Cashier + " · Shift " + Shift;
    }

    private static double[] Mz(params double[] a) => Enumerable.Range(0, 12).Select(i => i < a.Length ? a[i] : 0).ToArray();

    private static Sheet S(int day, string branch, string cashier, int shift, double[] m, double pab, double rei, double rf, double canx, double cash,
        double open, int depDay, double dep, string bank, string rem, string status) => new()
    {
        Day = day, Branch = branch, Cashier = cashier, Shift = shift, M = m, Pab = pab, Rei = rei, Ref = rf, Canx = canx, Cash = cash,
        Open = open, DepDay = depDay, Dep = dep, Bank = bank, Rem = rem, Status = status,
    };

    // The prototype's cash-sheet sample (SH), in its original order.
    private static readonly Sheet[] All =
    {
        S(5, "DEL001", "Motilal", 1, Mz(50000, 0, 0, 12000, 8000), 0, 0, 0, 0, 10000, 10000, 0, 0, "", "", "Pending"),
        S(5, "DEL001", "Ramesh", 2, Mz(0, 25000, 0, 0, 0, 0, 20000), 25000, -50000, 0, 25000, 5000, 10000, 0, 0, "", "Booked > cancelled & Reissued partial > booked", "Pending"),
        S(5, "HYD001", "Sowmya", 1, Mz(0, 0, 46500, 0, 22000, 0, 0, 0, 0, 15000), 0, 0, 0, 0, 18000, 12000, 5, 18000, "SBI HYD 5964", "", "Pending"),
        S(4, "DEL001", "Motilal", 3, Mz(40000, 0, 0, 0, 0), 0, 0, -50000, 0, 4000, 8000, 4, 8000, "CBI DELHI 9432", "Booked > cancelled & refund full > booked", "Approved"),
        S(4, "BLR001", "Karan", 1, Mz(0, 0, 0, 31000, 26000, 0, 0, 12000), 0, 0, 0, 0, 9000, 6000, 4, 9000, "SBI HYD 5964", "", "Approved"),
        S(4, "MAA001", "Divya", 1, Mz(0, 0, 0, 0, 18000, 16000, 0, 0, 0, 0, 9000), 0, -50000, 0, 30000, 6000, 8000, 5, 6000, "CBI DELHI 4362", "Cancelled & Reissued partial > not booked", "Pending"),
        S(3, "HYD001", "Sowmya", 2, Mz(0, 0, 52000, 8000, 14000), 0, 0, -25000, 25000, 7000, 5000, 3, 7000, "SBI HYD 5964", "Booked > cancelled & refund partial > booked", "Approved"),
        S(3, "DEL001", "Ramesh", 1, Mz(36000, 22000, 0, 0, 0, 0, 0, 6000), 0, 0, 0, 0, 2000, 8000, 3, 2000, "CBI DELHI 4362", "", "Approved"),
        S(2, "BLR001", "Karan", 2, Mz(0, 0, 0, 24000, 12000, 0, 0, 0, 0, 0, 18000), 20000, -50000, 0, 30000, 11000, 6000, 2, 11000, "SBI HYD 5964", "Cancelled & Reissued partial > booked", "Approved"),
        S(2, "MAA001", "Divya", 2, Mz(0, 0, 0, 0, 28000, 0, 0, 0, 0, 0, 0, 14000), 0, 0, 0, 0, 6000, 8000, 0, 0, "", "", "Pending"),
        S(1, "DEL001", "Motilal", 1, Mz(45000, 0, 0, 10000, 9000), 0, 0, 0, 0, 8000, 6000, 1, 8000, "CBI DELHI 9432", "", "Approved"),
        S(1, "HYD001", "Sowmya", 1, Mz(0, 0, 39000, 0, 16000), 50000, -50000, 0, 0, 5000, 4000, 1, 5000, "SBI HYD 5964", "Cancelled & Reissued > booked", "Approved"),
    };

    public static CashSheetAuditPage Build(ReportQuery q)
    {
        var view = Views.Contains(q.Tab) ? q.Tab! : "day";
        var led = view == "ledger";
        var page = new CashSheetAuditPage
        {
            Id = "r14",
            PagerUnit = "cash sheets",
            HasFilters = false,
            Title = "Cash Sheet Audit Report",
            Description = "Cash Sheet Audit Report — one row per cashier cash sheet: collections by payment mode (bank-deposit accounts, Pine Labs, payment links, online), payment against booking, reissue deposits, refunds, cancellation charges and cash, with total sales (excluding cancellation income) and the cash-management position (opening balance, deposits, closing balance and approval status). Click any amount to see the transactions behind it. Month view shows one row per branch per day with the month total on top. Branches see only their own sheets; Finance sees all.",
            Query = q,
            View = view,
            SearchPlaceholder = "Search serial no., cashier, branch...",
            HasExpand = !led,
            Tabs = Tabs,
            ActiveTab = led ? "ledger" : "day",
            TitleControls = "_CashSheetTitle",
            ToolbarExtra = "_CashSheetToolbar",
            Filters = new FilterDef[]
            {
                new("Cashier Branch", Branches),
                new("Cashier Name", new[] { "Motilal", "Ramesh", "Sowmya", "Karan", "Divya" }),
                new("Payment Mode", new[] { "CBI DELHI 9432", "CBI DELHI 4362", "SBI HYD 5964", "Pine Lab QR", "Pine Labs POS", "Instamojo", "RazorPay", "PayU", "Pine Lab", "Cash" }),
            },
            DateFields = new[] { "Date" },
            ViewOptions = led ? Array.Empty<CsKv>() : new CsKv[]
            {
                new("Matrix", "day", view == "day" ? "on" : ""),
                new("Rows", "rows", view == "rows" ? "on" : ""),
                new("Detail", "detail", view == "detail" ? "on" : ""),
            },
        };

        // Newest first, then branch, then shift (the default period, Last 5 Days, covers every sample sheet).
        var sheets = All.OrderByDescending(x => x.Date).ThenBy(x => x.Branch, StringComparer.Ordinal).ThenBy(x => x.Shift).ToList();

        if (led) BuildLedger(page, sheets);
        else
        {
            page.Kpis.AddRange(new[]
            {
                new Kpi("Cash sheets", sheets.Count.ToString(), Navy, true),
                new Kpi("Total sales", Fmt.Inr(sheets.Sum(x => x.Tot)), Navy, true),
                new Kpi("Cash collected", Fmt.Inr(sheets.Sum(x => x.Cash)), Blue, true),
                new Kpi("Refunds", Fmt.Inr(-sheets.Sum(x => x.Ref)), Red, true),
                new Kpi("Cancellation charges", Fmt.Inr(sheets.Sum(x => x.Canx)), Amb, true),
                new Kpi("Deposited in bank", Fmt.Inr(sheets.Sum(x => x.Dep)), Grn, true),
            });
            if (view == "day")
            {
                var (slice, pager) = Pager.Paginate(sheets, q.Page);
                page.Pager = pager;
                page.GridRows = slice.Select(GridRow).ToList();
            }
            else if (view == "rows") page.RowsItems = sheets.Select(RowsItem).ToList();
            else page.DetailItems = sheets.Select(DetailItem).ToList();
        }
        return page;
    }

    // ---------- formatting ----------

    private static string Fm(double v) => v == 0 ? "–" : v < 0 ? "−" + Fmt.Inr(-v) : Fmt.Inr(v);

    /// <summary>"05 Oct 26".</summary>
    private static string Sd(DateTime d) => Fmt.Date(d)[..^4] + (d.Year % 100).ToString("D2");

    /// <summary>"5 Oct 26" (ledger dates carry no leading zero).</summary>
    private static string Dl(DateTime d) => d.Day + " " + Fmt.Date(d).Substring(3, 3) + " " + (d.Year % 100).ToString("D2");

    private static CsStatus St(string status) => new(status, status == "Approved" ? "ok" : "pend");

    private static string ModeTitle(int i) => Mn[i] + (i > 8 ? " · online" : i > 4 && i < 9 ? " · payment link" : "");

    private static string ModeShort(int i) => Mn[i] + (i > 8 ? " (online)" : i > 4 && i < 9 ? " (link)" : "");

    // ---------- Cash Sheet View (grid) ----------

    private static CsAmt Amt(Sheet x, string title, double v) => v == 0
        ? new CsAmt("–", "c-mut", null)
        : new CsAmt(v < 0 ? "−" + Fmt.Inr(-v) : Fmt.Inr(v), (v < 0 ? "c-red" : "c-blue") + " fw6", title + " · " + x.Branch + " · " + x.Cashier + " · Shift " + x.Shift + " · " + Sd(x.Date));

    private static CsGridRow GridRow(Sheet x)
    {
        var a = Mn.Select((_, i) => Amt(x, ModeTitle(i), x.M[i])).ToList();
        a.Add(Amt(x, "Payment against booking", x.Pab));
        a.Add(Amt(x, "Reissue deposits", x.Rei));
        a.Add(Amt(x, "Refund", x.Ref));
        a.Add(Amt(x, "Cancellation charges", x.Canx));
        a.Add(Amt(x, "Cash", x.Cash));
        return new CsGridRow(Sd(x.Date), x.Branch, x.Cashier, "Shift " + x.Shift, x.Serial, a, Fmt.Inr(x.Tot));
    }

    // ---------- Rows view ----------

    private static IReadOnlyList<CsKv> Adjustments(Sheet x) => new (string L, double V)[] { ("Payment against booking", x.Pab), ("Reissue deposits", x.Rei), ("Refund", x.Ref), ("Cancellation charges", x.Canx) }
        .Where(a => a.V != 0).Select(a => new CsKv(a.L, Fm(a.V), a.V < 0 ? "c-red" : "c-blue")).ToList();

    private static CsRowsItem RowsItem(Sheet x)
    {
        var coll = new List<CsKv>();
        var g = "";
        for (var i = 0; i < 12; i++)
        {
            if (x.M[i] == 0) continue;
            if (ModeGroup[i] != g) { g = ModeGroup[i]; coll.Add(new CsKv(g, "", Group: true)); }
            coll.Add(new CsKv(Mn[i], Fm(x.M[i])));
        }
        if (x.Cash != 0) { coll.Add(new CsKv("Cash", "", Group: true)); coll.Add(new CsKv("Cash collected", Fm(x.Cash))); }
        if (coll.Count == 0) coll.Add(new CsKv("No collections", "–"));
        var adj = Adjustments(x);
        if (adj.Count == 0) adj = new[] { new CsKv("None", "–", "c-mut") };
        return new CsRowsItem(Sd(x.Date), x.Serial, x.Branch + " · " + x.Cashier + " · Shift " + x.Shift, coll, adj,
            Fm(x.Cash), Fmt.Inr(x.Tot), x.Dep != 0 ? Fmt.Inr(x.Dep) : "No deposit", x.Dep != 0 ? "on " + Sd(x.DepDate) : "", x.Bank,
            x.Dep != 0 ? "View slip" : x.Cash != 0 ? "Upload slip" : null, x.Status == "Pending",
            Fmt.Inr(x.Cash), Fmt.Inr(x.Open), Fmt.Inr(x.Close), Sd(x.Date), x.Rem.Length > 0 ? x.Rem : "–", x.Rem.Length > 0, St(x.Status));
    }

    // ---------- Detail view ----------

    private static CsKv Mode(Sheet x, int i) => x.M[i] != 0 ? new CsKv(Mn[i], Fm(x.M[i]), "c-blue fw6") : new CsKv(Mn[i], "–", "c-mut");

    private static CsDetailItem DetailItem(Sheet x)
    {
        var adj = new (string L, double V)[] { ("Payment against booking", x.Pab), ("Reissue deposits", x.Rei), ("Refund", x.Ref), ("Cancellation charges", x.Canx) }
            .Select(a => a.V != 0 ? new CsKv(a.L, Fm(a.V), (a.V < 0 ? "c-red" : "c-blue") + " fw6") : new CsKv(a.L, "–", "c-mut")).ToList();
        var stTone = x.Status == "Approved" ? "c-grn fw7" : "c-amb fw7";
        var mgmt = new List<CsKv>
        {
            new("Opening cash balance", Fmt.Inr(x.Open)),
            new("Cash", Fmt.Inr(x.Cash)),
            new("Entry date", Sd(x.Date)),
            new("Deposit date", x.Dep != 0 ? Sd(x.DepDate) : "–"),
            new("Deposit amount", x.Dep != 0 ? Fmt.Inr(x.Dep) : "–", "fw7"),
            new("Deposit in the bank", x.Bank.Length > 0 ? x.Bank : "–"),
            new("Deposit slip upload", x.Dep != 0 ? "View" : x.Cash != 0 ? "Upload" : "–", x.Dep != 0 || x.Cash != 0 ? "c-blue fw6" : "c-mut"),
            new("Details (remarks)", x.Rem.Length > 0 ? x.Rem : "–"),
            new("Closing cash balance", Fmt.Inr(x.Close), "fw7"),
            new("Status", x.Status, stTone),
        };
        if (x.Status == "Pending") mgmt.Add(new CsKv("Approval (level 1)", "Approve", "c-blue fw6"));
        var secs = new List<CsDetailSection>
        {
            new("Bank deposit modes", Enumerable.Range(0, 4).Select(i => Mode(x, i)).ToList()),
            new("POS", new[] { Mode(x, 4) }),
            new("Payment links (multipurpose GRP, w/o GST)", Enumerable.Range(5, 4).Select(i => Mode(x, i)).ToList()),
            new("Online – balance clearance & website", Enumerable.Range(9, 3).Select(i => Mode(x, i)).ToList()),
            new("Booking adjustments", adj),
            new("Cash & sales", new[]
            {
                x.Cash != 0 ? new CsKv("Cash", Fm(x.Cash), "c-blue fw6") : new CsKv("Cash", "–", "c-mut"),
                new CsKv("Total sales (excl. cancellation income)", Fmt.Inr(x.Tot), "c-navy fw8"),
            }),
            new("Cash management", mgmt),
        };
        return new CsDetailItem(Sd(x.Date), x.Serial, Fmt.Inr(x.Tot), x.Branch + " · " + x.Cashier, St(x.Status),
            x.Serial, x.Branch + " · " + x.Cashier + " · Shift " + x.Shift, secs);
    }

    // ---------- Cash Ledger ----------

    private static void BuildLedger(CashSheetAuditPage page, List<Sheet> sheets)
    {
        var lo = Day1;
        var hi = new DateTime(2026, 10, 5);
        var range = Dl(lo) + " – " + Dl(hi);
        double totOpen = 0, totIn = 0, totOut = 0, totClose = 0;
        var pending = 0;
        foreach (var br in Branches)
        {
            var ss = sheets.Where(x => x.Branch == br && x.Date >= lo && x.Date <= hi).OrderBy(x => x.Date).ThenBy(x => x.Shift).ToList();
            var ob = ss.Count > 0 ? ss[0].Open : 0;
            // Collections post on the entry date, deposits on the deposit date; collections first on the same day.
            var ev = ss.SelectMany(x => new[] { (K: 0, T: x.Date, X: x), (K: 1, T: x.DepDate, X: x) }.Where(e => e.K == 0 ? x.Cash != 0 : x.Dep != 0))
                .OrderBy(e => e.T).ThenBy(e => e.K).ToList();
            double bal = ob, tin = 0, tout = 0;
            var ent = new List<CsLedgerEntry>();
            foreach (var (k, t, x) in ev)
            {
                var dep = k == 1;
                if (dep) { bal -= x.Dep; tout += x.Dep; } else { bal += x.Cash; tin += x.Cash; }
                ent.Add(new CsLedgerEntry(Dl(t),
                    dep ? "Deposited in " + (x.Bank.Length > 0 ? x.Bank : "bank") : "Cash collected · " + x.Cashier + " · Shift " + x.Shift,
                    x.Serial + (x.Rem.Length > 0 ? " · " + x.Rem : "") + (dep ? "" : " · Entry " + Sd(x.Date)),
                    dep, dep ? "" : Fmt.Inr(x.Cash), dep ? Fmt.Inr(x.Dep) : "", Fmt.Inr(bal), St(x.Status), x.Status == "Pending"));
            }
            var pend = ss.Count(x => x.Status == "Pending");
            page.Branches.Add(new CsLedgerBranch(br, Fmt.Inr(bal), pend > 0 ? pend + " pending" : "Clear", pend > 0, Fmt.Inr(ob), Fmt.Inr(tin), Fmt.Inr(tout), range, Dl(lo), ent));
            totOpen += ob; totIn += tin; totOut += tout; totClose += bal; pending += pend;
        }
        page.Kpis.AddRange(new[]
        {
            new Kpi("Branches", page.Branches.Count.ToString(), Navy, true),
            new Kpi("Period", range, Navy),
            new Kpi("Opening balance", Fmt.Inr(totOpen), Navy, true),
            new Kpi("Cash collected", Fmt.Inr(totIn), Blue, true),
            new Kpi("Deposited in bank", Fmt.Inr(totOut), Grn, true),
            new Kpi("Closing balance", Fmt.Inr(totClose), Navy, true),
            new Kpi("Pending approval", pending.ToString(), pending > 0 ? Amb : Navy, true),
        });
    }
}
