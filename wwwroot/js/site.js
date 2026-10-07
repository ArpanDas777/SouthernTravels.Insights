// Southern Travels Insights — Operations reports (client behaviour).
// Report state lives in the query string; changing it re-requests the same Razor page and swaps
// every [data-frag] region in place. Row expansion, menus, the date-range calendar and toasts are client-only.
(function () {
  'use strict';

  const MON = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  const main = document.getElementById('main');
  let state = new URLSearchParams(location.search);
  let reqId = 0;

  // ---------- last report + per-report state ----------
  const store = {
    get(k) { try { return localStorage.getItem(k); } catch (e) { return null; } },
    set(k, v) { try { localStorage.setItem(k, v); } catch (e) { } },
  };
  const activeNav = document.querySelector('.sidebar a.nav.active');
  const currentPath = activeNav ? activeNav.pathname : location.pathname;

  // Opening the app root reopens the last report used.
  if (location.pathname === '/' && !location.search) {
    const last = store.get('ops.report');
    if (last && last !== currentPath) { location.replace(last + (store.get('ops.state:' + last) || '')); return; }
  }
  store.set('ops.report', currentPath);

  function saveState() { const qs = state.toString(); store.set('ops.state:' + currentPath, qs ? '?' + qs : ''); }
  saveState();

  // ---------- state & refresh ----------
  function set(key, value) {
    if (value === null || value === undefined || value === '') state.delete(key);
    else state.set(key, value);
  }

  function refresh() {
    const qs = state.toString();
    const url = location.pathname + (qs ? '?' + qs : '');
    history.replaceState(null, '', url);
    saveState();
    syncNavLinks();
    const id = ++reqId;
    fetch(url, { headers: { 'X-Requested-With': 'fetch' } })
      .then(r => r.text())
      .then(html => {
        if (id !== reqId) return;
        const doc = new DOMParser().parseFromString(html, 'text/html');
        doc.querySelectorAll('[data-frag]').forEach(next => {
          const cur = document.querySelector('[data-frag="' + next.dataset.frag + '"]');
          if (cur) cur.replaceWith(document.importNode(next, true));
        });
      })
      .catch(() => toast('Could not refresh the report. Please try again.', true));
  }

  // Sidebar links reopen each report with its own saved state; a report not yet visited inherits the Period.
  function syncNavLinks() {
    const p = new URLSearchParams();
    ['period', 'pf', 'pt'].forEach(k => { if (state.get(k)) p.set(k, state.get(k)); });
    const periodQs = p.toString();
    document.querySelectorAll('.sidebar a.nav').forEach(a => {
      const path = a.pathname;
      const saved = path === currentPath ? null : store.get('ops.state:' + path);
      a.href = path + (saved !== null ? saved : periodQs ? '?' + periodQs : '');
    });
  }
  syncNavLinks();

  // ---------- toast ----------
  let toastTimer;
  function toast(msg, err) {
    const t = document.getElementById('toast');
    t.textContent = msg;
    t.classList.toggle('err', !!err);
    t.hidden = false;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => { t.hidden = true; }, 2400);
  }

  // ---------- expandable rows ----------
  function setOpen(row, open) {
    row.classList.toggle('is-open', open);
    const drill = row.nextElementSibling;
    if (drill && drill.classList.contains('drill')) drill.hidden = !open;
  }

  function resultRows() {
    return document.querySelectorAll('[data-frag="results"] tr.row.expandable');
  }

  function closeWorkloadDrills() {
    document.querySelectorAll('[data-wl-drill]').forEach(d => { d.hidden = true; });
    document.querySelectorAll('.num-btn.on').forEach(b => b.classList.remove('on'));
    document.querySelectorAll('tr[data-user].is-open').forEach(r => r.classList.remove('is-open'));
  }

  // ---------- date-range calendar ----------
  let cal = null; // { btn, y, m }

  const iso = d => d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
  const parseIso = s => { if (!s) return null; const [y, m, d] = s.split('-').map(Number); return new Date(y, m - 1, d); };
  const fmt = d => String(d.getDate()).padStart(2, '0') + ' ' + MON[d.getMonth()] + ' ' + d.getFullYear();

  function rangeText(btn) {
    const from = parseIso(btn.dataset.from), to = parseIso(btn.dataset.to);
    btn.querySelector('span').textContent = from ? fmt(from) + ' – ' + (to ? fmt(to) : 'select end date') : 'Select date range';
    btn.classList.toggle('set', !!from);
  }

  function applyRange(btn) {
    const slug = btn.dataset.range;
    set('r_' + slug + '_from', btn.dataset.from);
    set('r_' + slug + '_to', btn.dataset.to);
    state.delete('page');
    rangeText(btn);
    refresh();
  }

  function closeCal() {
    document.querySelectorAll('.cal').forEach(c => c.remove());
    cal = null;
  }

  function renderCal() {
    document.querySelectorAll('.cal').forEach(c => c.remove());
    if (!cal) return;
    const { btn, y, m } = cal;
    const from = parseIso(btn.dataset.from), to = parseIso(btn.dataset.to);
    const box = document.createElement('div');
    box.className = 'cal';
    let html = '<div class="cal-head"><button type="button" class="cal-nav" data-cal="prev" aria-label="Previous month">‹</button>' +
      '<span class="cal-month">' + MON[m] + ' ' + y + '</span>' +
      '<button type="button" class="cal-nav" data-cal="next" aria-label="Next month">›</button></div>' +
      '<div class="cal-dow"><span>Mo</span><span>Tu</span><span>We</span><span>Th</span><span>Fr</span><span>Sa</span><span>Su</span></div><div class="cal-days">';
    const lead = (new Date(y, m, 1).getDay() + 6) % 7, dim = new Date(y, m + 1, 0).getDate();
    for (let k = 0; k < lead; k++) html += '<button type="button" class="cal-day blank" tabindex="-1"></button>';
    for (let n = 1; n <= dim; n++) {
      const t = new Date(y, m, n).getTime();
      const end = (from && t === from.getTime()) || (to && t === to.getTime());
      const mid = from && to && t > from.getTime() && t < to.getTime();
      html += '<button type="button" class="cal-day' + (end ? ' end' : mid ? ' mid' : '') + '" data-cal-day="' + iso(new Date(y, m, n)) + '">' + n + '</button>';
    }
    html += '</div><div class="cal-foot"><button type="button" class="cal-clear" data-cal="clear">Clear</button>' +
      '<button type="button" class="cal-done" data-cal="done">Done</button></div>';
    box.innerHTML = html;
    btn.parentElement.appendChild(box);
  }

  function toggleCal(btn) {
    if (cal && cal.btn === btn) { closeCal(); return; }
    const d = parseIso(btn.dataset.from) || new Date(2026, 9, 5);
    cal = { btn, y: d.getFullYear(), m: d.getMonth() };
    renderCal();
  }

  function pickDay(isoDay) {
    const btn = cal.btn;
    const d = parseIso(isoDay), from = parseIso(btn.dataset.from);
    if (!from || btn.dataset.to) { btn.dataset.from = isoDay; btn.dataset.to = ''; }
    else if (d < from) { btn.dataset.to = btn.dataset.from; btn.dataset.from = isoDay; }
    else btn.dataset.to = isoDay;
    applyRange(btn);
    renderCal();
  }

  // ---------- actions menu (Ticket-Wise rows) ----------
  const menu = document.getElementById('actions-menu');
  let menuTimer, menuTicket = null;

  function openMenu(btn) {
    clearTimeout(menuTimer);
    const no = btn.dataset.menu, ok = btn.dataset.vouchers === '1';
    menuTicket = no;
    const items = [
      ['Download Ticket', true, 'Ticket downloaded'],
      ['Download Itinerary', true, 'Itinerary downloaded'],
      ['View Cost Sheet', true, 'Cost sheet opened'],
      ['Download Vouchers', ok, 'Vouchers downloaded (ZIP)'],
    ];
    menu.innerHTML = items.map(([label, en, msg]) =>
      '<button type="button" class="' + (en ? '' : 'off') + '" data-menu-run="' + (en ? msg : '') + '">' + label + '</button>').join('');
    const b = btn.getBoundingClientRect();
    menu.style.left = Math.max(8, b.right - 200) + 'px';
    menu.style.top = (b.bottom + 2) + 'px';
    menu.hidden = false;
  }

  function scheduleMenuClose() {
    clearTimeout(menuTimer);
    menuTimer = setTimeout(() => { menu.hidden = true; }, 200);
  }

  document.addEventListener('mouseover', e => {
    const btn = e.target.closest('[data-menu]');
    if (btn && !btn.contains(e.relatedTarget)) openMenu(btn);
    if (e.target.closest('#actions-menu')) clearTimeout(menuTimer);
  });
  document.addEventListener('mouseout', e => {
    const from = e.target.closest('[data-menu], #actions-menu');
    if (from && !from.contains(e.relatedTarget)) scheduleMenuClose();
  });

  // ---------- clicks ----------
  document.addEventListener('click', e => {
    const el = e.target;

    // Menu items
    const run = el.closest('[data-menu-run]');
    if (run) {
      menu.hidden = true;
      const msg = run.dataset.menuRun;
      if (msg) toast('✓ ' + msg + ' – ' + menuTicket);
      else toast('Vouchers unavailable – all services must be completed first.', true);
      return;
    }
    if (el.closest('[data-menu]')) { e.stopPropagation(); return; }

    // Voucher buttons (do not toggle the row)
    const voucher = el.closest('.voucher');
    if (voucher) { toast(voucher.dataset.toast, voucher.dataset.toastErr === '1'); return; }

    // Calendar
    const calDay = el.closest('[data-cal-day]');
    if (calDay && cal) { pickDay(calDay.dataset.calDay); return; }
    const calCmd = el.closest('[data-cal]');
    if (calCmd && cal) {
      const c = calCmd.dataset.cal;
      if (c === 'prev') { cal.m = (cal.m + 11) % 12; if (cal.m === 11) cal.y--; renderCal(); }
      else if (c === 'next') { cal.m = (cal.m + 1) % 12; if (cal.m === 0) cal.y++; renderCal(); }
      else if (c === 'clear') { cal.btn.dataset.from = ''; cal.btn.dataset.to = ''; applyRange(cal.btn); renderCal(); }
      else closeCal();
      return;
    }
    if (el.closest('.cal')) return;
    const rangeBtn = el.closest('[data-range]');
    if (rangeBtn) { toggleCal(rangeBtn); return; }

    // Tabs: reset expansion and paging for every view
    const tab = el.closest('[data-tab]');
    if (tab) { set('tab', tab.dataset.tab); state.delete('page'); refresh(); return; }

    // Pagination
    const pg = el.closest('[data-page]');
    if (pg) { if (!pg.disabled && pg.dataset.page) { set('page', pg.dataset.page); refresh(); } return; }

    // OPS Workload: count drill-down (one open at a time)
    const wl = el.closest('[data-wl]');
    if (wl) {
      const key = wl.dataset.wl, drill = document.querySelector('[data-wl-drill="' + CSS.escape(key) + '"]');
      const wasOpen = drill && !drill.hidden;
      closeWorkloadDrills();
      if (drill && !wasOpen) { drill.hidden = false; wl.classList.add('on'); wl.closest('tr').classList.add('is-open'); }
      return;
    }
    // OPS Workload: collapse a branch / a calendar week
    const banner = el.closest('tr.banner[data-branch]');
    if (banner) { toggleListParam('bc', banner.dataset.branch); refresh(); return; }
    const week = el.closest('[data-week]');
    if (week) { toggleListParam('wk', week.dataset.week); refresh(); return; }

    const action = el.closest('[data-action]');
    if (action) {
      switch (action.dataset.action) {
        case 'toggle-nav': {
          const collapsed = document.documentElement.classList.toggle('nav-collapsed');
          action.title = collapsed ? 'Expand menu' : 'Collapse menu';
          try { localStorage.setItem('ops.navCollapsed', collapsed ? '1' : '0'); } catch (err) { }
          return;
        }
        case 'clear-period': {
          ['period', 'pf', 'pt'].forEach(k => state.delete(k));
          const sel = document.querySelector('[data-action="period"]');
          if (sel) sel.value = 'All time';
          document.querySelectorAll('[data-action="pf"], [data-action="pt"]').forEach(i => { i.value = ''; });
          const custom = document.querySelector('[data-custom]');
          if (custom) custom.hidden = true;
          state.delete('page');
          refresh();
          return;
        }
        case 'toggle-filters': {
          const panel = document.getElementById('filter-panel');
          panel.hidden = !panel.hidden;
          action.classList.toggle('on', !panel.hidden);
          main.classList.toggle('filters-open', !panel.hidden);
          if (panel.hidden) closeCal();
          return;
        }
        case 'clear-all': {
          [...state.keys()].filter(k => k === 'q' || k === 'period' || k === 'pf' || k === 'pt' || k.startsWith('f_') || k.startsWith('r_')).forEach(k => state.delete(k));
          state.delete('page');
          const periodSel = document.querySelector('[data-action="period"]');
          if (periodSel) periodSel.value = 'All time';
          document.querySelectorAll('[data-action="pf"], [data-action="pt"]').forEach(inp => { inp.value = ''; });
          const customBox = document.querySelector('[data-custom]');
          if (customBox) customBox.hidden = true;
          const search = document.querySelector('[data-action="search"]');
          if (search) search.value = '';
          document.querySelectorAll('[data-filter]').forEach(s => { s.value = ''; });
          document.querySelectorAll('[data-range]').forEach(b => { b.dataset.from = ''; b.dataset.to = ''; rangeText(b); });
          closeCal();
          refresh();
          return;
        }
        case 'export': toast('✓ Report exported.'); return;
        case 'expand-all': resultRows().forEach(r => setOpen(r, true)); return;
        case 'collapse-all': resultRows().forEach(r => setOpen(r, false)); closeWorkloadDrills(); return;
        case 'show-all': set('all', action.dataset.value); state.delete('page'); refresh(); return;
      }
    }

    // Row expand / collapse
    const row = el.closest('tr.row.expandable');
    if (row) setOpen(row, !row.classList.contains('is-open'));
  });

  function toggleListParam(key, value) {
    const list = (state.get(key) || '').split(',').filter(Boolean);
    const i = list.indexOf(value);
    if (i >= 0) list.splice(i, 1); else list.push(value);
    set(key, list.join(','));
  }

  // ---------- inputs ----------
  let searchTimer;
  document.addEventListener('input', e => {
    if (e.target.matches('[data-action="search"]')) {
      clearTimeout(searchTimer);
      const v = e.target.value;
      searchTimer = setTimeout(() => { set('q', v); state.delete('page'); refresh(); }, 250);
    }
  });

  document.addEventListener('change', e => {
    const t = e.target;
    if (t.matches('[data-action="period"]')) {
      const custom = document.querySelector('[data-custom]');
      if (custom) custom.hidden = t.value !== 'Custom';
      set('period', t.value === 'All time' ? '' : t.value);
      if (t.value !== 'Custom') { state.delete('pf'); state.delete('pt'); }
      state.delete('page');
      refresh();
    } else if (t.matches('[data-action="wf"], [data-action="wt"]')) {
      state.set(t.dataset.action, t.value); // kept even when empty: an empty end means "open range"
      refresh();
    } else if (t.matches('[data-action="pf"], [data-action="pt"]')) {
      set(t.dataset.action, t.value);
      state.delete('page');
      refresh();
    } else if (t.matches('[data-filter]')) {
      set(t.dataset.filter, t.value);
      state.delete('page');
      refresh();
    }
  });
})();
