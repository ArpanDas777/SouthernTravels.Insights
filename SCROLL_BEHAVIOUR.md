# Report page scrolling: KPI cards scroll away, tabs and search stay pinned

A reusable pattern for any list page that has a KPI row, view tabs, a search / filter toolbar and a long table. Pure HTML and CSS, no JavaScript. It comes from the Ticket & Service Status report of the Southern Travels Insights project (ASP.NET Core MVC), but nothing in it depends on that stack.

## 1. What the user sees

1. The page opens with the KPI cards at the top, then the tabs, the toolbar (search, Filters, Export) and the table.
2. Scrolling down moves the whole page. The **KPI cards scroll out of view**.
3. The **tabs and the toolbar stop at the top of the content area and stay there**, so search and filters are always one click away.
4. The **table header row also stays pinned**, directly under the toolbar.
5. The page header (logo, search, bell) and the side menu never move.
6. If the filter panel is open it belongs to the pinned block, so it stays visible with the toolbar.

The alternative this replaces is the "fixed shell" mode, where only the table card scrolls inside the screen and the KPIs, tabs and toolbar never move. Both modes are described below because the pattern is an opt-in switch on top of the fixed shell.

## 2. The two layers

### 2.1 Fixed shell (always on)

The app fills the viewport and does not scroll. Only `main` scrolls.

```css
.app   { height: 100vh; overflow: hidden; }
.shell { display: flex; align-items: stretch; height: calc(100vh - 48px); }   /* 48px = header height */
.main  { flex: 1; min-width: 0; height: 100%; box-sizing: border-box;
         display: flex; flex-direction: column; overflow-y: auto; padding: 8px 20px; }

@supports (height: 100dvh) {          /* mobile browsers: the address bar changes 100vh */
  .app   { height: 100dvh; }
  .shell { height: calc(100dvh - 48px); }
}
```

Rule: **the header height is written in three places** (header, `.shell`, side menu `top` and `height`). Change them together.

### 2.2 Default mode: the table card scrolls

```css
.results { display: flex; flex-direction: column; flex: 1 1 0; min-height: 0; }
.card    { overflow: auto; flex: 1 1 0; min-height: 300px; background: #fff; }
.gt > thead th { position: sticky; top: 0; z-index: 3; }      /* header pinned inside the card */
```

`.results` takes the leftover height and the card scrolls inside it. KPIs, tabs and toolbar are `flex: none`, so they never scroll. This is right for dense grids and calendars.

### 2.3 Page-scroll mode (this document's feature)

An opt-in class on `main` changes three things: the table card stops scrolling, `main` scrolls the whole page, and a wrapper pins the tabs and toolbar.

```css
/* tabs + toolbar (+ filter panel) pinned at the top of main */
.sticky-bar { position: sticky; top: -8px; z-index: 12;
              background: var(--page); padding-top: 8px; flex: none; }
.sticky-bar .tabs { margin-top: 0; }

/* the card no longer owns the scroll; main does */
.main.page-scroll .results { flex: none; min-height: 0; }
.main.page-scroll .card    { overflow: visible; flex: none; min-height: 0; }

/* table header pinned just under the bar */
.main.page-scroll .gt > thead th { top: var(--stick-h, 64px); }
```

Why each line exists:

| Line | Reason |
|---|---|
| `position: sticky; top: -8px` on `.sticky-bar` | `main` has 8px top padding. A sticky `top` is measured from the scroll container's edge, so `-8px` lets the bar sit flush with the top of the content area instead of leaving an 8px gap. The bar's own `padding-top: 8px` keeps the same spacing at rest. |
| `background: var(--page)` | The bar must be opaque, or the table rows show through it as they scroll underneath. |
| `z-index: 12` | Above table header cells (z 3 to 5) and sticky columns, below the side menu (25), drop-downs (45) and modals (60). |
| `overflow: visible` on `.card` | A sticky element sticks to its **nearest scrolling ancestor**. If the card keeps `overflow: auto`, the header row would stick inside the card (which no longer scrolls) and never to the page. This is the most common reason the header "does not stick". |
| `--stick-h` | The pinned table header must start exactly where the bar ends. See section 5. |

## 3. Markup

One wrapper around the tabs, the toolbar and the filter panel. KPIs stay **outside** the wrapper, which is what lets them scroll away.

```html
<main class="main page-scroll">

  <div class="kpis"> ...KPI cards... </div>        <!-- scrolls away -->

  <div class="sticky-bar">                         <!-- pinned -->
    <div class="tabs"> ...tab links... </div>
    <input type="checkbox" id="filtertoggle" class="filter-chk" hidden>
    <div class="toolbar"> search, Filters, Export </div>
    <div class="filters"> ...filter panel... </div>
  </div>

  <div class="results">
    <div class="card"><table class="gt"> ...rows... </table></div>
    <nav class="pager"> ... </nav>
  </div>
</main>
```

Notes:

- The filter checkbox, toolbar and panel must stay siblings inside the wrapper, because the panel opens with `#filtertoggle:checked ~ .filters` (CSS only).
- Pages that do not want the behaviour render the same markup with the wrapper as `display: contents` (a class such as `.fc { display: contents }`) and without the `page-scroll` class. One template serves both modes.
- In the Razor project the switch is one boolean on the page model (`StickyHeader`), read by the layout (class on `main`) and by the toolbar partial (class on the wrapper).

## 4. Phones and tablets (below 1150px)

Tables become cards and the page scrolls normally, so the page-scroll mode already matches the default there:

```css
@media (max-width: 1149px) {
  .results { flex: none; min-height: 0; }
  .card:not(.keep-scroll) { overflow: visible !important; flex: none; border: 0; background: transparent; }
  .gt > thead tr:not(.weeks) { display: none; }          /* no header row in card layout */
}
```

- The pinned bar still works on phones. It is about 150px tall there because the search box takes its own row. If that is too much on small screens, pin only the tabs: add `@media (max-width: 767px) { .sticky-bar .toolbar { position: static } }` and move the toolbar out of the wrapper.
- The filter panel becomes a full-screen sheet (`position: fixed`), so it does not depend on the bar.
- Grids that must keep their pinned first column and horizontal scroll (calendars, wide matrices) add `keep-scroll` to the card and stay in the default mode.

## 5. Tuning `--stick-h`

`--stick-h` is the height at which the table header sticks. It has to equal the height of the pinned bar. Measure it once in the browser:

```js
const bar = document.querySelector('.sticky-bar');
Math.round(bar.getBoundingClientRect().height)      // tabs + toolbar, e.g. 72
```

Set `--stick-h` to that value minus the 8px main padding (here the bar measured 72px and `64px` lines the header up flush). Re-measure if you change the tab height, toolbar padding or font size. If the filter panel can open inside the bar and you want the header to follow it, that is the one case that needs a script; otherwise accept that the header sits under the closed bar.

## 6. Checklist for the other project

1. Make sure the app shell is fixed height with `main` as the only scroll container (section 2.1).
2. Wrap tabs, toolbar and filter panel in one `.sticky-bar`. Leave KPIs outside it.
3. Add the opt-in class to `main` and the three overrides from section 2.3.
4. Remove `overflow: auto` from every ancestor between the table and `main` (a single `overflow: hidden` or `auto` in between breaks sticky).
5. Give the bar an opaque background and a z-index above the table header.
6. Measure the bar and set `--stick-h`.
7. Test: scroll 500px, then read `getBoundingClientRect().top` of the bar (should equal the top of `main`) and of the KPI row (should be negative).
8. Test at phone width, with the filter panel open, and with a short window (the bar must not cover more than a third of the screen).

## 7. Known limits

- A sticky element cannot leave its parent. The bar's parent is `main`, so the bar is pinned for the whole page, which is what we want.
- Anchor links (`#id`) and `scrollIntoView` land under the bar. Add `scroll-margin-top` equal to the bar height on the targets.
- Browsers that do not support `position: sticky` simply scroll everything (no pinned bar). All current browsers support it.
- `:has()` is used elsewhere in the project for drill-down rows. The scroll pattern itself needs only `position: sticky`.
