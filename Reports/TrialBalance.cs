using System.Globalization;
using SouthernTravels.Insights.Data;
using SouthernTravels.Insights.Infrastructure;

namespace SouthernTravels.Insights.Reports;

/// <summary>A vendor in the Trial Balance list (the seven ticket vendors plus extra ledger-only vendors).</summary>
public sealed record TbVendor(string Id, string Name, string Type, string Currency);

/// <summary>One service as the ledger sees it: invoiced at start, paid at end, cancelled services carry a charge.</summary>
public sealed record TbLine(string No, string Mbk, string Type, string Name, DateTime Start, DateTime End, bool Cancelled, double Neg, double Sell);

public enum LedgerKind { Invoice, Paid, Cancel }

/// <summary>
/// One ledger entry. Invoice: Credit, dated at service start. Cancel: cancellation charges payable to the vendor
/// (Credit, at service start). Paid: one weekly payment to the vendor (Debit), listing the tickets it settles.
/// </summary>
public sealed record LedgerEntry(
    DateTime Date, LedgerKind Kind, string Line1, string Line2, IReadOnlyList<string> Tickets, double Debit, double Credit, double Balance);

/// <summary>Entries of one calendar month, shown under a month band ("OCTOBER 2026").</summary>
public sealed record LedgerMonth(string Label, List<LedgerEntry> Entries);

/// <summary>
/// Vendor ledger for a date range (Service wise Report → Trial Balance tab).
/// Balance is the running amount owed to the vendor: Cr (payable) is positive, Dr (receivable) negative.
/// Sample figures are derived from the sample services; payment details are generated placeholders.
/// </summary>
public sealed class TrialBalance
{
    public static readonly string[] Presets = { "Today", "This Month", "Last Month", "This Quarter", "Financial Year", "Custom" };
    public static readonly string[] FyLabels = { "FY 2026–27", "FY 2025–26", "FY 2024–25" };
    public static readonly string[] TypeFilters = { "All service types", "Accommodation", "Transport", "Guide", "En-Route Meals", "Other" };
    private static readonly string[] Types = { "Accommodation", "Transport", "Guide", "En-Route Meals", "Other" };
    private static readonly string[] Modes = { "NEFT", "RTGS", "UPI", "Cheque" };
    private static readonly DateTime Epoch = new(1970, 1, 1);

    public string Preset { get; init; } = "Financial Year";
    public required string Fy { get; init; }
    /// <summary>"01 Apr 2026 – 31 Mar 2027", shown in the range chip.</summary>
    public required string RangeText { get; init; }
    public string TypeFilter { get; init; } = TypeFilters[0];
    /// <summary>Text in the vendor box ("Taj Falaknuma Palace (V105)").</summary>
    public required string VendorText { get; init; }
    public string? VendorId { get; init; }
    public string VendorName { get; init; } = "";
    public string Currency { get; init; } = "INR";
    public string Period { get; init; } = "";
    public string OpeningDate { get; init; } = "";
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

    /// <summary>Vendors offered in the list. V101–V107 are the ticket vendors; V108–V112 only have ledger sample data.</summary>
    public static readonly IReadOnlyList<TbVendor> Vendors = new TbVendor[]
    {
        new("V101", "Hotel Clarks Shiraz", "Accommodation", "INR"),
        new("V102", "Royal Cabs Delhi", "Transport", "INR"),
        new("V103", "Heritage Guides Jaipur", "Guide", "USD"),
        new("V104", "Spice Route Caterers", "En-Route Meals", "EUR"),
        new("V105", "Taj Falaknuma Palace", "Accommodation", "INR"),
        new("V106", "Deccan Travels", "Transport", "INR"),
        new("V107", "Charminar Experiences", "Other", "USD"),
        new("V108", "Rajasthan Heritage Stays", "Accommodation", "INR"),
        new("V109", "Kerala Backwater Cruises", "Other", "INR"),
        new("V110", "Dubai Desert Safari LLC", "Transport", "USD"),
        new("V111", "Alpine Tours Switzerland", "Accommodation", "EUR"),
        new("V112", "Goa Beach Resorts", "Accommodation", "INR"),
    };

    private static List<TbLine> LinesOf(string vid)
    {
        if (SampleData.Vendors.ContainsKey(vid))
            return ServiceRow.All().Where(x => x.S.VendorId == vid)
                .Select(x => new TbLine(x.T.No, x.T.Mbk, x.S.Type, x.S.Name, x.S.Start, x.S.End, x.S.Status == "Cancelled", x.Negotiated, x.S.Sell)).ToList();

        // Ledger-only vendors: eight deterministic services from April to November, one of them cancelled.
        var v = Vendors.First(x => x.Id == vid);
        var seed = int.Parse(vid[1..], CultureInfo.InvariantCulture);
        var lines = new List<TbLine>();
        for (var i = 0; i < 8; i++)
        {
            var start = new DateTime(2026, 4, 8).AddDays(i * 31 + seed % 7 * 2);
            var nights = 2 + (i + seed) % 4;
            var neg = (28000 + (seed * 3 + i * 7) % 9 * 6500) * (v.Currency == "INR" ? 1 : 83);
            var n = 1000 + (seed * 211 + i * 73) % 8000;
            lines.Add(new TbLine((n % 2 == 1 ? "GRP" : "SPL") + n, "MBK-" + (89000 + seed * 10 + i), v.Type,
                v.Name + " – " + (v.Type == "Accommodation" ? "Suite" : v.Type == "Transport" ? "Full-day transfer" : "Experience"),
                start, start.AddDays(nights), i == 4, neg, Fmt.Round(neg * 1.18 / 100) * 100));
        }
        return lines;
    }

    /// <summary>Sample ledger shown on the tab for one vendor (default Taj Falaknuma Palace) over FY 2026–27; the other controls are visual only.</summary>
    public static TrialBalance Build(string? vendorId = null)
    {
        DateTime lo = new(2026, 4, 1), hiDay = new(2027, 3, 31), hiExcl = hiDay.AddDays(1);
        var range = Fmt.Date(lo) + " – " + Fmt.Date(hiDay);

        var v = Vendors.FirstOrDefault(x => string.Equals(x.Id, vendorId, StringComparison.OrdinalIgnoreCase)) ?? Vendors.First(x => x.Id == "V105");
        var vid = v.Id;

        var tb = new TrialBalance
        {
            Fy = FyLabels[0],
            RangeText = range,
            VendorText = v.Name + " (" + v.Id + ")",
            VendorName = v.Name,
            VendorId = vid,
            Currency = v.Currency,
            Period = range,
            OpeningDate = Fmt.Date(lo),
        };

        var all = LinesOf(vid);

        // Opening balance brought forward: 35% of what was invoiced before the period (Cr).
        tb.Opening = Fmt.Round(all.Where(x => x.Start < lo).Sum(x => x.Neg) * .35 / 100) * 100;

        // Invoices and cancellation charges (Credit) are dated at service start; payments to the vendor (Debit) are
        // grouped by week of service end. Weeks run Friday to Thursday, which is how the prototype buckets
        // epoch-day / 7 in India Standard Time.
        var events = new List<(DateTime Date, int Kind, LedgerEntry Entry)>();
        var weeks = new SortedDictionary<long, List<(TbLine X, double Amount)>>();
        foreach (var x in all.Where(x => x.Start >= lo && x.Start < hiExcl))
        {
            if (x.Cancelled)
            {
                var basis = x.Neg != 0 ? x.Neg : x.Sell * .8;
                var charge = Fmt.Round(basis * .25 / 100) * 100;
                events.Add((x.Start, 2, new LedgerEntry(x.Start, LedgerKind.Cancel, x.No + " / " + x.Mbk + " · Cancelled",
                    "Cancellation charges payable to vendor (25% of " + tb.Money(basis) + ") · " + x.Type + " · " + x.Name, Array.Empty<string>(), 0, charge, 0)));
                continue;
            }
            events.Add((x.Start, 0, new LedgerEntry(x.Start, LedgerKind.Invoice, x.No + " / " + x.Mbk,
                x.Type + " · " + x.Name + " · " + Fmt.Date(x.Start) + " to " + Fmt.Date(x.End), Array.Empty<string>(), 0, x.Neg, 0)));
            var pf = .45 + (Math.Max(Array.IndexOf(Types, x.Type), 0) * 7 % 5) / 20.0;
            var week = (long)Math.Floor(((x.End.Date - Epoch).Days - 1) / 7.0);
            if (!weeks.TryGetValue(week, out var list)) weeks[week] = list = new();
            list.Add((x, Fmt.Round(x.Neg * pf / 100) * 100));
        }
        foreach (var list in weeks.Values)
        {
            var last = list.Select(p => p.X.End).Max();
            var first = list[0].X;
            var refNo = DigitsOf(first.No) + DigitsOf(first.Mbk) + "26";
            refNo = refNo[Math.Max(0, refNo.Length - 9)..];
            var mode = Modes[(first.No.Length + Fmt.Date(last).Length) % 4];
            events.Add((last, 1, new LedgerEntry(last, LedgerKind.Paid,
                "Paid to vendor · " + list.Count + (list.Count > 1 ? " tickets" : " ticket"),
                mode + " · Ref " + refNo + " · Bank ••4821",
                list.Select(p => p.X.No + " / " + p.X.Mbk + " · " + tb.Money(p.Amount)).ToList(), list.Sum(p => p.Amount), 0, 0)));
        }

        var entries = events.Where(e => e.Date < hiExcl).OrderBy(e => e.Date).ThenBy(e => e.Kind).Select(e => e.Entry).ToList();

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

    /// <summary>Digits of a ticket / MBK number (a character sum when there are none).</summary>
    private static string DigitsOf(string s)
    {
        var d = new string(s.Where(char.IsDigit).ToArray());
        return d.Length > 0 ? d : s.Sum(c => c).ToString(CultureInfo.InvariantCulture);
    }
}
