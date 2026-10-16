using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Infrastructure;

/// <summary>Small markup helpers for the stacked "line" cells, badges and pills used by every report table.</summary>
public static class Ui
{
    private static string E(object? v) => HtmlEncoder.Default.Encode(v?.ToString() ?? "");

    /// <summary>Status badge tone (ticket / service / OPS / approval / payment statuses).</summary>
    public static string Tone(string status) => status switch
    {
        "Confirmed" or "Approved" or "Paid" or "Refund" or "Active" or "Generated" => "tone-ok",
        "Cancel" => "tone-cancel",
        "Pending" or "Not generated" => "tone-warn",
        "Cancelled" or "Rejected" => "tone-danger",
        "In Progress" or "Requested" or "Reissue" => "tone-info",
        "Completed" => "tone-done",
        _ => "tone-neutral",
    };

    public static string TypeTone(string type) => type switch
    {
        "Accommodation" => "ty-acc",
        "Transport" => "ty-tra",
        "Guide" => "ty-gui",
        "En-Route Meals" => "ty-mea",
        _ => "ty-oth",
    };

    public static IHtmlContent Badge(string status, string cls = "") =>
        new HtmlString($"<span class=\"bdg {Tone(status)} {cls}\">{E(status)}</span>");

    public static IHtmlContent Pill(string type, string cls = "") =>
        new HtmlString($"<span class=\"bdg {TypeTone(type)} {cls}\">{E(type)}</span>");

    public static IHtmlContent Count(object n) => new HtmlString($"<span class=\"cnt\">{E(n)}</span>");

    /// <summary>One stacked line inside a cell.</summary>
    public static IHtmlContent Ln(object? text, string cls = "", string? tip = null) =>
        new HtmlString($"<div class=\"ln\"><span class=\"{cls}\"{(tip is null ? "" : $" title=\"{E(tip)}\"")}>{E(text)}</span></div>");

    /// <summary>Wraps already-built markup (badge/pill) as a stacked line.</summary>
    public static IHtmlContent LnHtml(IHtmlContent inner, string cls = "") =>
        new HtmlContentBuilder().AppendHtml($"<div class=\"ln {cls}\">").AppendHtml(inner).AppendHtml("</div>");

    /// <summary>Two stacked lines: a semibold primary line over a smaller muted line.</summary>
    public static IHtmlContent Two(object? a, object? b, string aCls = "")
    {
        var cls = aCls.Contains("fw") ? aCls : "fw6 " + aCls;
        return new HtmlContentBuilder().AppendHtml(Ln(a, cls)).AppendHtml(Ln(b, "fs105 c-mut"));
    }

    /// <summary>File number that wraps at its slashes (FN/000007/2026-27/DEL001).</summary>
    public static IHtmlContent FileNo(string file, string cls = "") =>
        new HtmlString($"<div class=\"ln\"><span class=\"{cls}\">{string.Join("/<wbr>", file.Split('/').Select(p => E(p)))}</span></div>");

    /// <summary>Drill-down confirmation date (DD/MM/YY), or amber "Pending" when not yet confirmed.</summary>
    public static IHtmlContent Conf(DateTime? conf) =>
        conf is null ? Ln("Pending", "c-amb fw7") : Ln(Fmt.Short(conf.Value));

    /// <summary>Grand / Paid / Balance stacked figures, with paid % and due %.</summary>
    public static IHtmlContent GrandPaidBalance(Ticket t)
    {
        var b = new HtmlContentBuilder()
            .AppendHtml(Ln(Fmt.Inr(t.Grand), "fw7"))
            .AppendHtml(Ln(Fmt.Inr(t.Paid) + " paid · " + t.PaidPctText, "c-grn"));
        return b.AppendHtml(t.Balance > 0 ? Ln(Fmt.Inr(t.Balance) + " due · " + t.DuePctText, "fw7 c-red") : Ln("Nil · 0%", "c-grn"));
    }

    /// <summary>Drill-down Start / "to" / End / "{N}d" (DD/MM/YY).</summary>
    public static IHtmlContent StartEnd(DateTime start, DateTime end, int days) =>
        new HtmlContentBuilder()
            .AppendHtml(Ln(Fmt.Short(start)))
            .AppendHtml(Ln("to", "fs9 c-mut"))
            .AppendHtml(Ln(Fmt.Short(end)))
            .AppendHtml(Ln(days + "d", "fw7"));

    /// <summary>Compact Start – End on one line with the duration below.</summary>
    public static IHtmlContent StartEndCompact(DateTime start, DateTime end, int days) =>
        new HtmlContentBuilder()
            .AppendHtml(Ln(Fmt.Short(start) + " – " + Fmt.Short(end), "nowrap"))
            .AppendHtml(Ln(days + "d", "fs10 c-mut"));
}
