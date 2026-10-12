using System.Globalization;
using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

/// <summary>One ledger entry: an invoice (Credit, dated at service start) or a payment to the vendor (Debit, dated at service end).</summary>
public sealed record LedgerEntry(
    DateTime Date, bool IsPayment, string TicketNo, string Line1, string Line2, double Debit, double Credit, double Balance);

/// <summary>Entries of one calendar month, shown under a month band ("OCTOBER 2026").</summary>
public sealed record LedgerMonth(string Label, List<LedgerEntry> Entries);

/// <summary>
/// Vendor ledger for a financial-year month range (Service wise Report → Trial Balance tab).
/// Balance is the running amount owed to the vendor: Cr (payable) is positive, Dr (receivable) negative.
/// Sample figures are derived from the sample services; payment details are generated placeholders.
/// </summary>
public sealed class TrialBalance
{
    public static readonly string[] Months = { "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Jan", "Feb", "Mar" };
    public static readonly string[] FyLabels = { "FY 2026–27", "FY 2025–26", "FY 2024–25" };
    private static readonly string[] Types = { "Accommodation", "Transport", "Guide", "En-Route Meals", "Other" };
    private static readonly string[] Modes = { "NEFT", "RTGS", "UPI", "Cheque" };

    public required string Fy { get; init; }
    public required string FromMonth { get; init; }
    public required string ToMonth { get; init; }
    /// <summary>Text in the vendor box ("Hotel Clarks Shiraz (V101)").</summary>
    public required string VendorText { get; init; }
    public string? VendorId { get; init; }
    public string Currency { get; init; } = "INR";
    public string Period { get; init; } = "";
    public double Opening { get; private set; }
    public double TotDebit { get; private set; }
    public double TotCredit { get; private set; }
    public double Closing { get; private set; }
    public List<LedgerMonth> MonthsList { get; } = new();

    public int EntryCount => MonthsList.Sum(m => m.Entries.Count);
    public bool HasLedger => VendorId is not null;

    /// <summary>Closing balance as printed in the bar and KPI card.</summary>
    public string ClosingAmountText => Money(Math.Abs(Closing));
    public string ClosingNote => Closing > 0 ? "Cr — payable" : Closing < 0 ? "Dr — receivable" : "Nil";
    public string ClosingCls => Closing > 0 ? "c-red" : "c-grn";
    public string ClosingColor => Closing > 0 ? "var(--color-danger)" : "var(--ok-ink)";

    private double Rate => Currency switch { "USD" => 83, "EUR" => 90, _ => 1 };
    private string Sym => Currency switch { "USD" => "$", "EUR" => "€", _ => "₹" };

    /// <summary>Amount in the vendor's currency, Indian digit grouping (up to 2 decimals for foreign currency).</summary>
    public string Money(double n)
    {
        var v = n / Rate;
        var nf = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nf.NumberGroupSizes = new[] { 3, 2 };
        return Sym + v.ToString(Rate > 1 ? "#,##0.##" : "#,##0", nf);
    }

    /// <summary>Balance with its marker: "₹1,17,700 Cr" (payable) / "₹2,000 Dr" (receivable).</summary>
    public string Bal(double b) => Money(Math.Abs(b)) + (b > 0 ? " Cr" : b < 0 ? " Dr" : "");

    /// <summary>
    /// Dr = Cr check. A Cr (payable) closing balance is the balancing figure on the Dr side
    /// (Opening + Credit = Debit + Closing); a Dr closing balance sits on the Cr side.
    /// </summary>
    public double DrSide => TotDebit + (Closing > 0 ? Closing : 0);
    public double CrSide => Opening + TotCredit + (Closing < 0 ? -Closing : 0);
    public bool Balanced => Math.Abs(DrSide - CrSide) < 0.5;

    /// <summary>Sample ledger shown on the tab: one fixed vendor, FY 2026–27, Apr – Oct (the controls are visual only).</summary>
    public static TrialBalance Build()
    {
        var fyIdx = 0;
        var fy = 2026 - fyIdx;
        int Idx(string m, string def) { var i = Array.IndexOf(Months, m); return i >= 0 ? i : Array.IndexOf(Months, def); }
        var a = Idx("Apr", "Apr");
        var b = Idx("Oct", "Oct");
        int mf = Math.Min(a, b), mt = Math.Max(a, b);
        DateTime Md(int i) => new(fy + (i >= 9 ? 1 : 0), (3 + i) % 12 + 1, 1);
        DateTime lo = Md(mf), hiExcl = Md(mt).AddMonths(1);

        const string text = "Taj Falaknuma Palace (V105)";
        var vq = text.Trim().ToLowerInvariant();
        var ids = SampleData.Vendors.Keys.ToList();
        var vid = ids.FirstOrDefault(id =>
        {
            var name = SampleData.VendorName(id);
            return id.ToLowerInvariant() == vq || name.ToLowerInvariant() == vq || (name + " (" + id + ")").ToLowerInvariant() == vq;
        });

        var tb = new TrialBalance
        {
            Fy = FyLabels[fyIdx],
            FromMonth = Months[mf],
            ToMonth = Months[mt],
            VendorText = text,
            VendorId = vq.Length > 0 ? vid : null,
            Currency = vid is null ? "INR" : new[] { "INR", "INR", "USD", "EUR" }[ids.IndexOf(vid) % 4],
            Period = Months[mf] + " " + (mf >= 9 ? fy + 1 : fy) + " – " + Months[mt] + " " + (mt >= 9 ? fy + 1 : fy),
        };
        if (tb.VendorId is null) return tb;

        var all = ServiceRow.All().Where(x => x.S.VendorId == vid).ToList();

        // Opening balance brought forward: 35% of what was invoiced before the period (Cr).
        tb.Opening = Fmt.Round(all.Where(x => x.S.Start < lo).Sum(x => x.Negotiated) * .35 / 100) * 100;

        // Credit = invoiced by the vendor, dated at service start.
        var entries = all.Where(x => x.S.Start >= lo && x.S.Start < hiExcl)
            .Select(x => (Date: x.S.Start, Pay: false, x.T.No, Entry: new LedgerEntry(x.S.Start, false, x.T.No,
                x.T.No + " / " + x.T.Mbk,
                x.S.Type + " · " + x.S.Name + " · " + Fmt.Short(x.S.Start) + " to " + Fmt.Short(x.S.End),
                0, x.Negotiated, 0)))
            // Debit = paid to the vendor by the company, dated at service end; only payments inside the period appear.
            .Concat(all.Where(x => x.S.End >= lo && x.S.End < hiExcl)
                .Select(x => (Date: x.S.End, Pay: true, x.T.No, Entry: Payment(x))))
            .OrderBy(e => e.Date).ThenBy(e => e.Pay).ThenBy(e => e.No, StringComparer.Ordinal)
            .Select(e => e.Entry)
            .ToList();

        // Running balance after every entry (Cr positive).
        var bal = tb.Opening;
        for (var i = 0; i < entries.Count; i++)
        {
            bal += entries[i].Credit - entries[i].Debit;
            entries[i] = entries[i] with { Balance = bal };
        }
        tb.TotDebit = entries.Sum(e => e.Debit);
        tb.TotCredit = entries.Sum(e => e.Credit);
        tb.Closing = bal;

        foreach (var g in entries.GroupBy(e => new { e.Date.Year, e.Date.Month }))
            tb.MonthsList.Add(new LedgerMonth(
                new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(), g.ToList()));
        return tb;
    }

    private static LedgerEntry Payment(ServiceRow x)
    {
        var ti = Array.IndexOf(Types, x.S.Type);
        var pf = .45 + (Math.Max(ti, 0) * 7 % 5) / 20.0;
        var amount = Fmt.Round(x.Negotiated * pf / 100) * 100;
        // Placeholder payment details, derived from the ticket number so they never change between loads.
        var n = DigitsOf(x.T.No);
        var mode = Modes[n % Modes.Length];
        var line2 = mode + " · " + mode + "-26-" + (n % 10000).ToString("D4") + " · A/c ••••" + (1000 + n * 37 % 9000) + " · against " + x.T.No;
        return new LedgerEntry(x.S.End, true, x.T.No, "Paid to vendor", line2, amount, 0, 0);
    }

    /// <summary>Stable number from a ticket no (digits, else a character sum).</summary>
    private static int DigitsOf(string s)
    {
        var d = new string(s.Where(char.IsDigit).ToArray());
        return d.Length > 0 && int.TryParse(d, out var v) ? v : s.Sum(c => c);
    }
}
