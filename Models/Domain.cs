namespace SouthernTravels.Insights.Models;

public sealed record Vendor(string Id, string Name, string Type);

public sealed record Service
{
    public required string Type { get; init; }
    public required string City { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Meal { get; init; }
    public required string Ops { get; init; }
    public required string OpsBranch { get; init; }
    /// <summary>Raw OPS status as stored (New / In Progress / Completed / Pending).</summary>
    public required string OpsStatus { get; init; }
    /// <summary>Service (supplier) status: Confirmed / Pending / Cancelled.</summary>
    public required string Status { get; init; }
    public DateTime? Conf { get; init; }
    public required string ConfNo { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public int Days { get; init; }
    public required string VendorId { get; init; }
    public double Sell { get; init; }
    public double Payable { get; init; }
    public double VendorBalance { get; init; }
    /// <summary>Days from service creation to confirmation (null when not confirmed).</summary>
    public int? Tat { get; init; }

    /// <summary>OPS status as displayed outside the Ticket-Wise drill-down ("Pending" reads as "In Progress").</summary>
    public string OpsDisplay => OpsStatus == "Pending" ? "In Progress" : OpsStatus;

    public bool VoucherReady => OpsStatus == "Completed" && Status == "Confirmed";
}

public sealed record Ticket
{
    public int Index { get; init; }
    public required string No { get; init; }
    public required string Mbk { get; init; }
    public required string File { get; init; }
    public required string PaxName { get; init; }
    public required string Tour { get; init; }
    public int Pax { get; init; }
    public required string Status { get; init; }
    public DateTime Booked { get; init; }
    public required string Exec { get; init; }
    public required string Branch { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public int Days { get; init; }
    public double Grand { get; init; }
    public double Paid { get; init; }
    public required IReadOnlyList<Service> Services { get; init; }

    public double Balance => Grand - Paid;
    /// <summary>Paid / balance as whole-number percentages of the grand total ("54%").</summary>
    public string PaidPctText => Math.Round(Paid / Grand * 100, MidpointRounding.AwayFromZero) + "%";
    public string DuePctText => Math.Round(Balance / Grand * 100, MidpointRounding.AwayFromZero) + "%";

    /// <summary>Overall ticket OPS status: all New → New, all Completed → Completed, otherwise In Progress.</summary>
    public string OverallOps =>
        Services.All(s => s.OpsStatus == "New") ? "New" :
        Services.All(s => s.OpsStatus == "Completed") ? "Completed" : "In Progress";

    public bool VouchersReady => Services.All(s => s.VoucherReady);

    public bool IsAssignedTo(string opsUser) => Services.Any(s => s.Ops == opsUser);
}
