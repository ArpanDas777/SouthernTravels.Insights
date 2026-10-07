# Southern Travels Insights — Operations reports

ASP.NET Core MVC (.NET 8) implementation of the "Ticket & Service Status" design handoff, covering all six Operations reports.

## Run

```
dotnet run --urls http://localhost:5080
```

Open http://localhost:5080 (defaults to Ticket & Service Status).

| Report | Route | View |
|---|---|---|
| Ticket & Service Status | `/Operations/TicketServiceStatus` | `Views/Operations/TicketServiceStatus.cshtml` |
| OPS User Workload | `/Operations/OpsUserWorkload` | `Views/Operations/OpsUserWorkload.cshtml` |
| Service wise Report | `/Operations/ServiceWise` | `Views/Operations/ServiceWise.cshtml` |
| Vendor Payment details | `/Operations/VendorPayment` | `Views/Operations/VendorPayment.cshtml` |
| Cancellation Report | `/Operations/Cancellation` | `Views/Operations/Cancellation.cshtml` |
| Advance Report | `/Operations/Advance` | `Views/Operations/Advance.cshtml` |

Shared views: `Views/Shared/_Layout.cshtml` (header + sidebar), `_ReportHeader.cshtml` (Period control + title),
`_KpisTabsToolbar.cshtml` (KPIs, view tabs, toolbar, filter panel), `_Pager.cshtml`.

## How it fits together

- **Data** — `Data/SampleData.cs` holds the prototype's sample tickets/services verbatim. Swap it for a real repository later; the report builders only read `SampleData.Tickets` / `SampleData.Vendors`.
- **Report logic** — `Reports/*Report.cs` filter, group and compute KPIs in C# and hand typed view models to the Razor views.
- **State** — every report setting lives in the query string (`tab`, `page`, `q`, `period`/`pf`/`pt`, `all`, `f_*` dropdowns, `r_*_from`/`r_*_to` date ranges, `wf`/`wt`/`wk`/`bc` for OPS Workload), so any view is bookmarkable.
- **Client** — `wwwroot/js/site.js` updates the query string, re-requests the same page and swaps the `[data-frag]` regions without a full reload. Row expand/collapse, the actions menu, the range calendar and toasts are client-only.
- **Styling** — `wwwroot/css/tokens.css` (design-system tokens, unchanged) + `wwwroot/css/site.css`. Fonts: Public Sans + Inter from Google Fonts. Light theme only.

## Decisions taken from the handoff

- Where the brief and the prototype disagree, the prototype (`Ticket & Service Status.dc.html`) wins.
- The prototype's filters were visual only; here they actually filter, using the brief's matching rules.
- Export, voucher and ticket downloads show a toast only (no files), as in the prototype.
- Text overlaps marked in the sketches (fixed-height cells) are fixed; no columns were added.

## CHANGES.md (6 Oct 2026) applied

- Header label and tab title stay "Southern Insights" (overrides the log's "Operations"); rail stays design-system navy.
- Last report reopens from `/` (`ops.report`); each report keeps its own search/filters/period (`ops.state:{path}`).
- "Clear all" also shows for an active Period and resets it; "Clear filters" sits at the top of the filter panel.
- Pagination (10/page, always visible) on every list table except OPS User Workload.
- Drill-down dates are DD/MM/YY; file numbers wrap at the slashes; no fixed cell widths; no sideways scroll down to 1280px.
- Ticket-Wise: Financials = balance → paid → total; tour name 8px. Vendor-Wise drill shows paid % and due %.
- Vendor Payment vendor drill: 17 columns (Passenger / Pax, Tour Start / Dur., No. / Category, Conf. Date / No.), service type as plain text.
- Cancellation: KPIs 2 × 5; Passenger / Pax and Cancel Profit / % merged; Cancelled By shows branch code; final charge split into charge / GST (5%) / Charge Basis; vendor view shows Vendor Name / ID, plain-text service type, no Cancel Profit column and no customer summary in the drill-down.
- Advance: headers "Passenger / Pax", "Booking Exec / Branch".
- Date-range filters keep working on every report (not only Ticket & Service Status).
- OPS Workload calendar legend unchanged.
