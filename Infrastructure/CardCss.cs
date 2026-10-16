using System.Text;
using Microsoft.AspNetCore.Html;

namespace SouthernTravels.Insights.Infrastructure;

/// <summary>
/// Phone / tablet card layout for one report table, as plain CSS (no script). Each column heading becomes the label
/// before its value. Cells are numbered from 1, matching the table's columns. Use it in the view as
/// <c>@CardCss.For("t-duty", cols, title: 1, lead: -1, extras: new[] { 3, 5 })</c> and give the table the classes
/// <c>cards t-duty</c>.
/// </summary>
public static class CardCss
{
    /// <param name="cls">The table's own class, e.g. "t-duty".</param>
    /// <param name="headers">One heading per column; an empty heading prints no label.</param>
    /// <param name="title">1-based column used as the card title (-1 = none).</param>
    /// <param name="lead">1-based chevron / marker column pinned to the card's top-right (-1 = none).</param>
    /// <param name="extras">1-based columns hidden until the card is opened with its chevron.</param>
    public static IHtmlContent For(string cls, IReadOnlyList<string> headers, int title = -1, int lead = -1, IEnumerable<int>? extras = null)
    {
        var cell = $".{cls} > tbody > tr:not(.drill):not(.banner) > td:nth-child(";
        const string Tail = "):not([colspan])";
        var sb = new StringBuilder("<style>@media (max-width:1149px){");
        for (var i = 0; i < headers.Count; i++)
        {
            var n = i + 1;
            if (n == title || n == lead || headers[i].Length == 0) continue;
            sb.Append(cell).Append(n).Append(Tail).Append("::before{content:\"").Append(headers[i].Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\"}");
        }
        if (title > 0)
            sb.Append(cell).Append(title).Append(Tail).Append("{display:block!important;text-align:left!important;padding-right:30px!important;font-size:13px}")
              .Append(cell).Append(title).Append(Tail).Append("::before{display:none!important}")
              .Append(cell).Append(title).Append(Tail).Append(">div,").Append(cell).Append(title).Append(Tail).Append(" .ln{text-align:left}");
        if (lead > 0)
            sb.Append(cell).Append(lead).Append(Tail).Append("{position:absolute!important;top:10px;right:10px;display:block!important;width:auto!important;padding:0!important}")
              .Append(cell).Append(lead).Append(Tail).Append("::before{display:none!important}");
        var ex = extras?.ToList() ?? new List<int>();
        if (ex.Count > 0)
            sb.Append(string.Join(",", ex.Select(n => $".{cls} > tbody > tr.row:not(:has(.rx:checked)) > td:nth-child({n})"))).Append("{display:none!important}");
        return new HtmlString(sb.Append("}</style>").ToString());
    }
}

/// <summary>
/// Column alignment for a report table from its headings, as the design prototype does: status columns centred,
/// money columns right, name / id columns left, date / count columns centred. Headings that match nothing keep
/// whatever the cell already uses. Emits plain CSS (no script); use next to <see cref="CardCss"/>.
/// </summary>
public static class AlignCss
{
    private static readonly System.Text.RegularExpressions.Regex Count = new(@"^(tickets?|services|users|vendors|pax|days|count|active)$|total (services|duration|pax|days|tickets|vendors|users)");
    private static readonly System.Text.RegularExpressions.Regex Money = new(@"amount|payable|paid|balance|price|value|gst|tcs|discount|commission|refund|advance|selling|cost|margin|gross|target|variation|collected|actual|net |sales?\b|₹|profit|invoice|billed|receivable|outstanding|total|contracted|negotiated|cancellations|\bvar\b|achv");
    private static readonly System.Text.RegularExpressions.Regex Text = new(@"ticket|passenger|name|vendor|exec|customer|narration|remark|expense|pan\b|employee|file no|mbk|reason|section|city|cities|branch|tour name|detail|particular");
    private static readonly System.Text.RegularExpressions.Regex Mid = new(@"date|dob|journey|completion|days|pax|achiev|%|approval|duration|active|action|slip|final bill|payment|period|start|end\b");

    /// <summary>"left" / "center" / "right", or null when the heading says nothing.</summary>
    public static string? Of(string heading)
    {
        var s = heading.ToLowerInvariant();
        if (s.Trim().Length == 0) return null;
        if (s.Contains("status") || Count.IsMatch(s)) return "center";
        if (Money.IsMatch(s)) return "right";
        if (Text.IsMatch(s)) return "left";
        return Mid.IsMatch(s) ? "center" : null;
    }

    /// <param name="cls">The table's own class, e.g. "t-adv".</param>
    /// <param name="headers">One heading per column in order (1-based nth-child).</param>
    /// <param name="extra">Columns whose heading says nothing but the prototype aligns explicitly: (1-based column, "left" / "center" / "right").</param>
    public static IHtmlContent For(string cls, IReadOnlyList<string> headers, params (int Col, string Align)[] extra)
    {
        var sb = new StringBuilder("<style>");
        for (var i = 0; i < headers.Count; i++)
        {
            var a = extra.Where(e => e.Col == i + 1).Select(e => e.Align).FirstOrDefault() ?? Of(headers[i]);
            if (a is null) continue;
            var td = $".{cls} > tbody > tr.row > td:nth-child({i + 1})";
            sb.Append($"{td},{td} .ln,{td} > div{{text-align:{a}!important}}");
        }
        return new HtmlString(sb.Append("</style>").ToString());
    }
}
