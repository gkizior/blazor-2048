// Injected by PerfTrace (test-side only; the app itself ships no custom JS).
(() => {
  const P = window.__perf = { frames: [], keys: [], muts: [], paints: [], longtasks: [], samples: [], sampling: false, cells: null };
  // Key-to-paint: the first DOM change after a key schedules one rAF; its time is the start of the
  // frame that paints the change.
  let paintPending = false;
  // The rAF loop only runs in sampling passes: a permanent rAF callback would force a main-thread
  // frame (and animation style updates) every vsync and distort the timing pass.
  let looping = false;
  const loop = t => {
    if (!P.sampling) { looping = false; return; }
    P.frames.push(t);
    sample(t);
    requestAnimationFrame(loop);
  };
  P.startSampling = () => { P.sampling = true; if (!looping) { looping = true; requestAnimationFrame(loop); } };
  document.addEventListener('keydown', e => P.keys.push(performance.now()), true);
  try {
    new PerformanceObserver(l => l.getEntries().forEach(e => P.longtasks.push([e.startTime, e.duration])))
      .observe({ type: 'longtask', buffered: true });
  } catch { }
  const mo = new MutationObserver(() => {
    P.muts.push(performance.now());
    if (!paintPending) { paintPending = true; requestAnimationFrame(() => { paintPending = false; P.paints.push(performance.now()); }); }
  });
  const attach = () => {
    const layer = document.querySelector('.tile-layer');
    if (layer) mo.observe(layer, { subtree: true, childList: true, attributes: true, attributeFilter: ['style', 'class'] });
    else setTimeout(attach, 50);
  };
  attach();
  const round = v => Math.round(v * 10) / 10;
  function sample(t) {
    const layer = document.querySelector('.tile-layer');
    if (!layer) return;
    const L = layer.getBoundingClientRect();
    if (!P.cells) P.cells = [...document.querySelectorAll('.board .cell')].map(c => {
      const r = c.getBoundingClientRect(); return [round(r.x + r.width / 2 - L.x), round(r.y + r.height / 2 - L.y), round(r.width)];
    });
    const tiles = [];
    for (const el of layer.children) {
      const inner = el.firstElementChild;
      const r = inner.getBoundingClientRect();
      const cls = el.className;
      tiles.push([+el.dataset.id, +el.dataset.value, +el.dataset.row, +el.dataset.col,
        cls.includes('retired') ? 'R' : cls.includes('merged') ? 'M' : cls.includes('new') ? 'N' : '',
        round(r.x + r.width / 2 - L.x), round(r.y + r.height / 2 - L.y), round(r.width),
        round(+getComputedStyle(inner).opacity * 100) / 100]);
    }
    P.samples.push([t, tiles]);
  }
})();
