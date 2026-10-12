using System.Net;
using Microsoft.AspNetCore.Html;
using SouthernTravels.Insights.Reports;

namespace SouthernTravels.Insights.Infrastructure;

/// <summary>Table-cell markup for the sales reports (Daily EMP Sales, Sales vs Target).</summary>
public static class SalesHtml
{
    private static string E(string s) => WebUtility.HtmlEncode(s);

    private static IHtmlContent Td(string cls, string inner) =>
        new HtmlString("<td class=\"" + cls + "\"><div class=\"ln\">" + inner + "</div></td>");

    private static string Span(string cls, string text) => "<span class=\"" + cls + "\">" + E(text) + "</span>";

    /// <summary>Amount cell: "-" for nothing / zero, otherwise ₹ with Indian grouping.</summary>
    public static IHtmlContent Amt(double? v, string cls = "", bool weekend = false) =>
        Td(weekend ? "amt we" : "amt", v is null || v == 0 ? Span("c-mut", "-") : Span(cls, Fmt.Inr(v.Value)));

    /// <summary>Achievement pill (▲ ≥100% green, ◆/▼ amber 60–99%, ▼ red below 60%), or "-" without a target.</summary>
    public static IHtmlContent Ach(AchCell? a) =>
        Td("achc", a is null ? Span("c-mut fs10", "-") : Span("ach " + a.Tone, a.Text));

    /// <summary>Sticky name cell, optionally with a second muted line (the employee's branch).</summary>
    public static IHtmlContent Lab(string name, string? sub, string cls, string style = "", bool toggle = false)
    {
        // "toggle" adds a CSS-only checkbox that opens the card on phones (hidden on desktop).
        var rx = toggle ? "<label class=\"rx-l rx-m\"><input type=\"checkbox\" class=\"rx\" aria-label=\"Show details\" /><span class=\"rx-chev\">▸</span></label>" : "";
        var html = "<td class=\"lab\"" + (style.Length > 0 ? " style=\"" + style + "\"" : "") + ">" + rx + "<div class=\"ln\"><span class=\"" + cls + "\" style=\"display:block\">" + E(name) + "</span></div>";
        if (sub is not null) html += "<div class=\"ln\"><span class=\"fs95 c-mut\" style=\"display:block\">" + E(sub) + "</span></div>";
        return new HtmlString(html + "</td>");
    }

    public static IHtmlContent BlankLab() => new HtmlString("<td class=\"lab\" style=\"padding:2px 8px\"><div class=\"ln\"><span class=\"fs9\">&nbsp;</span></div></td>");

    public static IHtmlContent Label(string text, string cls) => Td("mlbl ta-l", Span(cls, text));

    /// <summary>Comparison value cell with an optional variance ("▲ +4.2%") after it.</summary>
    public static IHtmlContent Mc(string text, string cls, (string Text, string Cls)? variance = null) =>
        Td("mc", Span(cls, text) + (variance is { } v && v.Text.Length > 0 ? Span("t2 " + v.Cls, v.Text) : ""));

    /// <summary>Sales vs Target value cell.</summary>
    public static IHtmlContent Sv(double v, string cls = "") => Td("sv", v != 0 ? Span(cls, Fmt.Inr(v)) : Span("c-mut", "-"));

    /// <summary>Variation (actual − target) followed by the achievement % ("/ ▲ 105.0%").</summary>
    public static IHtmlContent VarAch(VarAchCell v) =>
        Td("sv", Span(v.VarCls, v.VarText) + (v.AchText is null ? "" : Span("t2 fs10 " + v.AchCls, v.AchText)));
}
