# Southern Travels Insights — Operations reports

ASP.NET Core MVC (.NET 8) implementation of the "Ticket & Service Status" design handoff, covering all fifteen Operations reports.

## Run

```
dotnet run --urls http://localhost:5080
```

Open http://localhost:5080 (defaults to Ticket & Service Status).

| Report | Route | Controller · View |
|---|---|---|
| Ticket & Service Status | `/Operations/TicketServiceStatus` | `TicketServiceStatusController` · `Views/TicketServiceStatus/Index.cshtml` |
| OPS User Workload | `/Operations/OpsUserWorkload` | `OpsUserWorkloadController` · `Views/OpsUserWorkload/Index.cshtml` |
| Service wise Report | `/Operations/ServiceWise` | `ServiceWiseController` · `Views/ServiceWise/Index.cshtml` |
| Vendor Payment details | `/Operations/VendorPayment` | `VendorPaymentController` · `Views/VendorPayment/Index.cshtml` |
| Cancellation Report | `/Operations/Cancellation` | `CancellationController` · `Views/Cancellation/Index.cshtml` |
| Advance Report | `/Operations/Advance` | `AdvanceController` · `Views/Advance/Index.cshtml` |
| Weekly Sales trend | `/Operations/WeeklySalesTrend` | `WeeklySalesTrendController` · `Views/WeeklySalesTrend/Index.cshtml` |
| Daily EMP Sales | `/Operations/DailyEmpSales` | `DailyEmpSalesController` · `Views/DailyEmpSales/Index.cshtml` |
| Sales vs Target | `/Operations/SalesVsTarget` | `SalesVsTargetController` · `Views/SalesVsTarget/Index.cshtml` |
| Duty Slip Report | `/Operations/DutySlip` | `DutySlipController` · `Views/DutySlip/Index.cshtml` |
| TCS Report | `/Operations/Tcs` | `TcsController` · `Views/Tcs/Index.cshtml` |
| GST Report | `/Operations/Gst` | `GstController` · `Views/Gst/Index.cshtml` |
| TDS Report | `/Operations/Tds` | `TdsController` · `Views/Tds/Index.cshtml` |
| Cash Sheet Audit Report | `/Operations/CashSheetAudit` | `CashSheetAuditController` · `Views/CashSheetAudit/Index.cshtml` |
| Balance Pending Report | `/Operations/BalancePending` | `BalancePendingController` · `Views/BalancePending/Index.cshtml` |

Service wise Report also has a **Trial Balance** tab (`?tab=tb`, controls in `Views/ServiceWise/_TrialBalanceControls.cshtml`).
Sales figures for the three sales reports come from `Data/SalesSample.cs` (seeded sample generator, matches the prototype).

Shared views (`Views/Shared`): `_Layout.cshtml` (header + sidebar), `_ReportHeader.cshtml` (Period control + title),
`_KpisTabsToolbar.cshtml` (KPIs, view tabs, toolbar, filter panel), `_Pager.cshtml`.

## Project structure

Same layout as STPLCostSheet: one project, plain folders.

```
Controllers/     AccountController (login), BaseController, OperationsController (/Operations, /Operations/Error)
                 one controller per report, e.g. GstController -> /Operations/Gst -> Views/Gst/Index.cshtml
Services/        AccountService, OperationService      (call the API)
Interfaces/      IAccountService, IOperationService
Common/          ApiEndpoints, BaseApiService, SessionMiddleware, GlobalExceptionMiddleware, ApiErrorResponse
Helpers/         ApiClientHelper (POST to the API)
Security/        SessionSecurityService, SessionKeys
Models/          ApiResponseViewModel, ReportFilters, Domain, Auth/LoginModels
Reports/         one builder per report + ReportPage (page chrome, side menu) + CommonFilters
Data/            SampleData, SalesSample (sample report data)
Infrastructure/  Fmt, ReportQuery, Ui, CardCss, SalesHtml, RangePicker
Views/           Shared/ (_Layout, _ReportHeader, _KpisTabsToolbar, _Pager, _TaxFoot, _RangePicker), Account/Login, <Report>/Index
```

## How the API is called (same as STPLCostSheet)

```
Controller  ->  Service (: BaseApiService)  ->  ApiClientHelper.PostAsync<ApiResponseViewModel>  ->  API
                URL = STPLCostSheetAPI:ApiUrl + controller + "/" + action  (names in Common/ApiEndpoints.cs)
```

Example, the report filters (`Services/OperationService.cs`):

```csharp
var response = await _apiClient.PostAsync<ApiResponseViewModel>(
    BuildUrl(ApiEndpoints.Reports, ApiEndpoints.ReportActions.GetOperationsReportFilters),
    new { tenantID = tenantId, userId = session.UserName, userName = session.UserName, securityKey = session.Token });
```

- `TicketServiceStatusController` and `ServiceWiseController` call `GetOperationsReportFiltersAsync()` and pass `result.Data` to `CommonFilters.TicketServiceFrom(...)`, which turns the API lists into the dropdowns (value = the API id/code, text = the name). Service Type is not in the API, so it keeps the fixed list.
- If the API fails, the page still opens with the built-in sample lists.
- To add an API call: add the action name to `ApiEndpoints`, a method to `IOperationService` / `OperationService`, and the data to `ApiResponseViewModel`.

## Login (OTP, same as STPLCostSheet)

Every page needs a login. Without one, `Common/SessionMiddleware.cs` redirects to `/Account/Login?returnUrl=…`.

1. Enter your email → **Send OTP** calls `Account/SendOTP`.
2. Enter the 6-digit OTP → **Verify & Login** calls `Account/ValidateUserOTP`.
3. On success the session gets `UserId`, `UserName`, `Email`, `HandlingBranch`, `accessToken`, `refreshToken`, `IsAdmin`, `TenantId`, `LoginTime`.

- The signed-in name, role and branch show at the bottom of the side menu, with a log-out icon.
- The session lasts 30 minutes; Send OTP and Verify are limited to 5 tries per minute per IP.

## How it fits together

- **Data** — `Data/SampleData.cs` holds the prototype's sample tickets/services verbatim. Swap it for a real repository later; the report builders only read `SampleData.Tickets` / `SampleData.Vendors`.
- **Report builders** — `Reports/*Report.cs` turn the sample data into display-ready page models; views only print values (no calculations in `.cshtml`).
- **URL state** — only `tab`, `page` and `all` (Show all) are read from the query string; tabs and paging are plain links.
- **No JavaScript on the reports** — there is no `site.js` (only `login.js` on the login page). Row drill-downs, the sidebar, the filter panel, the Actions menu and the phone card details use CSS only (checkboxes, `:has()`, hover/focus).
- **Styling** — `wwwroot/css/tokens.css` (design-system tokens, unchanged) + `wwwroot/css/site.css`. Fonts: Public Sans + Inter from Google Fonts. Light theme only.

## Decisions taken from the handoff

- Where the brief and the prototype disagree, the prototype (`Ticket & Service Status.dc.html`) wins.
- This build is UI-only: search, filters, Period, calendars, the vendor picker, Export and similar controls are visual and do not change data.
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

## Design handoff of 12 Oct 2026 applied

- Shell: header "Southern Travels  Insights" (user block moved to the bottom of the sidebar), sidebar section "Reports"/"REP", nine items, auto-collapses after picking a report or clicking the page; below 768px it becomes a slide-out menu opened from the header ☰.
- Compact restyle across all reports (tighter cells, smaller KPI cards, 18px titles, flat tabs, centred table cells).
- Kept from earlier decisions: Cancellation "Cancelled By" shows name · branch code; Vendor Payment drill-down keeps 17 columns; pagination on all list tables; date ranges filter every report.
- Taken from the new prototype: Ticket Financials Balance → Total → Paid; Workload legend "Weekend columns are shaded" with a Status column; Advance headers "Passenger Info" / "Booking Executive"; KPI grids auto-fit (Cancellation no longer fixed 2 × 5).
- Weekly Sales trend: heatmap + line chart only (no pager / weekly tab).
- Daily EMP Sales: the prototype's narrow-window "compact week" font fitting is not reproduced; day columns use a fixed 9px.

## Responsive behaviour

| Width | Layout |
|---|---|
| 1920+ | Content capped at 1,600 px and centred. |
| 1280–1919 | Full desktop layout; the sidebar starts open. |
| 768–1149 (tablet) | Sidebar starts as the icon rail. List reports become cards; compact toolbar (icon buttons) and filters open as a full-screen sheet. |
| < 768 (phone) | As tablet, plus: slide-out menu from the header ☰, two-column KPI cards, Weekly Sales heatmap shows the date and net sales only (tap a day for the detail), 16px form fields (no iOS zoom). |

- Card lists: static CSS (`CardCss.For` in each view) prints each column heading before its value; only the key fields show until a card is opened with its chevron. If a column changes, update that view's `CardCss.For` headings.
- Sideways-scrolling grids (names pinned): the OPS Workload calendar and the Daily EMP Sales Comparison tab (`keep-scroll` / `data-nocards`). Sales vs Target scrolls sideways on desktop only; below 1150 px it is a card per executive.
- Verified with no page-level horizontal scroll at 360, 430, 768, 820, 1024, 1149, 1150, 1280, 1366, 1920 and 2560 px.

## Trial Balance ledger (TRIAL_BALANCE_CHANGES.md, 12 Oct 2026)

- Tab order: Vendor Wise, Service City-wise, **Trial Balance**. The old account-head table and its drill-down are removed.
- Toolbar (after the search box): searchable Vendor dropdown, Financial year, From month, to, To month, Print. The Period selector, Expand All and Collapse All are hidden on this tab; Export stays. The search box and Filters button are kept but do not affect the ledger.
- Ledger "Statement": opening balance (brought forward) row, then entries grouped under month bands (OCTOBER 2026). Columns: Date, Type (Invoice / Paid pill), Particular, Debit (Paid to vendor), Credit (Invoiced), Balance with Cr / Dr. Entries are ordered by date, invoice before payment, then ticket no.
- Credit = vendor invoice dated at service start; Debit = payment dated at service end (only payments inside the period). Payment mode, reference and bank last 4 are placeholders derived from the ticket number (`Reports/TrialBalance.cs`).
- Opening balance = 35% of invoices dated before the period (Cr).
- Closing-balance bar below the table (Closing balance, total Debit, total Credit, closing amount with "Cr — payable" / "Dr — receivable"); only the rows between the sticky header and the bar scroll.
- Dr = Cr check: Cr side = Opening + Credit; Dr side = Debit + Closing (a Cr closing balance is the balancing figure on the Dr side). The KPIs and the bar read the same totals.
- Phone / tablet: one card per entry, month bands as headings, closing bar pinned at the bottom.

## UI-only state (frontend logic removed)

- Everything shows the existing sample data in its default state; no script runs in the browser.
- Not reachable by design: week / quarter column expand, month-on-month comparison, metric highlight switch, group collapse, calendar picker, vendor picker, and any search / filter / Period effect.
- Trial Balance shows one fixed sample vendor ledger (Taj Falaknuma Palace, FY 2026-27, Apr–Oct); its controls are visual only.

## Handoff of 15 Oct 2026 applied

- Six new reports (r10–r15), built from the prototype's sample data; KPIs and footers match the prototype exactly. They are UI-only like the rest.
- Cash Sheet Audit: the Matrix / Rows / Detail switch and the Cash Sheet / Cash Ledger tabs are links (`?tab=`); picking a sheet or a branch uses CSS-only radios (first one selected). Amount pop-ups and modals are not built.
- Sample data: generated tickets use GRP / SPL numbers, and three confirmed tickets per vendor V101–V107 were added (80 services in all).
- The Period selector sits in the title row (with "Last 5 Days"); the tax, duty-slip and balance reports preselect "This Month", Cash Sheet Audit "Last 5 Days".
- Column alignment follows the prototype's header rule (`AlignCss` in `Infrastructure/CardCss.cs`): status centred, money right, names left, dates / counts centred.
- Trial Balance: Period preset, Financial year, range chip, Service type and Vendor controls; ledger has Invoice, Paid (weekly, listing its tickets) and Cancel entries. The Filters button is hidden on this tab and on Cash Sheet Audit.
- New nav icons, a scrolling rail, a narrower chevron column and scroll-capped drill-down tables.

## Weekly changes doc applied

- Weekly Sales trend now has three views as links (`?tab=calendar` default, `line`, `table`). Table lists Monday–Sunday weeks with totals; each week opens with a CSS-only checkbox and the current week starts open.
- The rest of the doc was already in the app, or is interactive behaviour (pop-ups, Approve, Send payment link, multi-select dropdowns) that UI-only mode leaves out. Ticket-Wise keeps its OPS Status and Service columns (the 15 Oct prototype has them; the doc's list lags it).

## Round of 16 Oct 2026 (feedback list)

- Side menu: an icon rail on every report, opening over the page on hover or focus (CSS only); the toggle pins it open.
- Ticket & Service Status: the page scrolls, KPI cards scroll away, tabs and toolbar stay pinned (`StickyHeader`).
- Service wise: Vendor Wise and the City-wise services table use combined cells (ticket / MBK / file, vendor + id, type + city, category + meal plan); Tour Price and Sell / Con / Neg are `.kv` label-value grids. Trial Balance has centred headers, a one-line toolbar, a CSS vendor list (`?tab=tb&vendor=V1xx`) and five more ledger-only vendors (V108–V112, `TrialBalance.Vendors`).
- OPS User Workload: Period sits in the title row, branch banners collapse their users (the subtotal stays), calendar header offsets and pinned columns fixed.
- Vendor Payment: vendor drill-down and ticket-wise tables merged as requested; Start / End is compact on the ticket drill-down.
- Card labels for the restructured tables now come from `CardCss.For` in the views (`cards.css` is gone).

## Side menu (per SIDE_MENU.md)

- Desktop: the rail starts open (216px). Picking a report links to `?pick=1`; `BaseController` sets a session cookie (`rail=c`) and redirects to the clean URL, so every page after that starts as the 56px icon rail (full width for the report). Tab and paging links keep the rail collapsed through the cookie.
- The toggle icon flips open / collapsed for the current page; a click on the page beside an open rail collapses it (a transparent scrim label for the same checkbox).
- Phones (<768px): hamburger opens the 260px drawer over a scrim; closes on scrim click or picking a report (unchanged).
- Not kept from the doc (needs script or stored state): restoring the last report after a refresh, per-report remembered tabs, and the toggle tooltip changing between "Collapse menu" / "Expand menu".
