// Browser helpers for the Nazor website: appearance, shortcuts, copy buttons, table of contents and diagrams.
window.nazorSite = (() => {
  const root = document.documentElement;
  const still = () => matchMedia('(prefers-reduced-motion: reduce)').matches;
  let initialPath = root.dataset.publicUrl ? location.pathname : null;
  const store = {
    get(key) { try { return localStorage.getItem(key); } catch { return null; } },
    set(key, value) { try { value == null ? localStorage.removeItem(key) : localStorage.setItem(key, value); } catch { } },
  };

  /* ---------- Appearance ---------- */
  function getPreferences() {
    return { theme: store.get('nazor-theme') || 'system', accent: store.get('nazor-accent') || 'blue' };
  }
  function setTheme(theme) {
    if (theme === 'light' || theme === 'dark') root.dataset.theme = theme; else delete root.dataset.theme;
    store.set('nazor-theme', theme === 'system' ? null : theme);
    renderDiagrams(true);
  }
  function setAccent(accent) {
    root.dataset.accent = accent;
    store.set('nazor-accent', accent === 'blue' ? null : accent);
  }

  /* ---------- Shortcuts ---------- */
  let shortcutTarget = null;
  function registerShortcuts(target) { shortcutTarget = target; }
  document.addEventListener('keydown', e => {
    const typing = e.target.closest && e.target.closest('input, textarea, [contenteditable="true"]');
    if (((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') || (!typing && e.key === '/')) {
      e.preventDefault();
      shortcutTarget && shortcutTarget.invokeMethodAsync('OpenSearch');
    }
  });

  /* ---------- Copy buttons ---------- */
  document.addEventListener('click', async e => {
    const button = e.target.closest('.code-copy');
    if (!button) return;
    const text = button.dataset.copy ?? button.closest('.code')?.querySelector('pre code')?.textContent ?? '';
    try {
      await navigator.clipboard.writeText(text);
      button.classList.add('done');
      const label = button.textContent.trim() ? button.textContent : null;
      if (label) button.textContent = 'Copied';
      setTimeout(() => { button.classList.remove('done'); if (label) button.textContent = label; }, 1400);
    } catch { }
  });

  /* ---------- Light sheen on press, instead of a circular ripple ---------- */
  document.addEventListener('pointerdown', e => {
    const b = e.target.closest('.btn'); if (!b || b.disabled || still()) return;
    const r = b.getBoundingClientRect(), s = document.createElement('span');
    s.className = 'sheen'; s.style.setProperty('--x', (e.clientX - r.left) + 'px'); s.style.setProperty('--y', (e.clientY - r.top) + 'px');
    b.appendChild(s); s.addEventListener('animationend', () => s.remove());
  });

  /* ---------- Compact header while scrolling ---------- */
  const onScroll = () => {
    root.classList.toggle('scrolled', scrollY > 8);
    spy();
  };
  let ticking = false;
  addEventListener('scroll', () => {
    if (ticking) return;
    ticking = true;
    requestAnimationFrame(() => { ticking = false; onScroll(); });
  }, { passive: true });

  /* ---------- Table of contents scroll spy ---------- */
  let headings = [];
  let activeId = null;
  function spy() {
    if (!headings.length) return;
    const line = 180;
    let current = headings[0];
    for (const h of headings) { if (h.getBoundingClientRect().top - line <= 0) current = h; else break; }
    if (current.id === activeId) return;
    activeId = current.id;
    const toc = document.querySelector('.toc');
    if (!toc) return;
    toc.querySelectorAll('a.active').forEach(a => a.classList.remove('active'));
    const link = toc.querySelector(`a[data-id="${CSS.escape(activeId)}"]`);
    if (link) {
      link.classList.add('active');
      const box = toc.querySelector('.toc-inner');
      if (box) {
        const lr = link.getBoundingClientRect(), br = box.getBoundingClientRect();
        if (lr.top < br.top + 40 || lr.bottom > br.bottom - 40) box.scrollTop += lr.top - br.top - br.height / 3;
      }
    }
  }

  /* ---------- Page lifecycle ---------- */
  function initialDocument() { return document.querySelector('#app article .doc')?.innerHTML ?? null; }
  function activateHead() {
    document.head.querySelectorAll('[data-static-seo]').forEach(tag => tag.remove());
    return root.dataset.publicUrl ?? null;
  }

  function pageRendered(resetScroll) {
    const openingPrerenderedPage = initialPath === location.pathname;
    initialPath = null;
    const hash = decodeURIComponent(location.hash.slice(1));
    const target = hash && document.getElementById(hash);
    if (target) target.scrollIntoView({ block: 'start', behavior: resetScroll || still() ? 'auto' : 'smooth' });
    else if (resetScroll && !openingPrerenderedPage) scrollTo({ top: 0, behavior: 'auto' });

    headings = [...document.querySelectorAll('.doc h2[id], .doc h3[id]')];
    activeId = null;
    spy();

    const nav = document.querySelector('.side .docnav-item.active');
    const side = document.querySelector('.side-inner');
    if (nav && side && resetScroll) {
      const nr = nav.getBoundingClientRect(), sr = side.getBoundingClientRect();
      if (nr.top < sr.top || nr.bottom > sr.bottom) side.scrollTop += nr.top - sr.top - sr.height / 3;
    }
    renderDiagrams(false);
  }

  /* ---------- Mermaid diagrams, loaded only when a page contains one ---------- */
  let mermaidLoad = null;
  function renderDiagrams(rerender) {
    const blocks = [...document.querySelectorAll('pre.mermaid')];
    if (!blocks.length) return;
    mermaidLoad ??= new Promise((resolve, reject) => {
      const s = document.createElement('script');
      s.src = 'https://cdn.jsdelivr.net/npm/mermaid@11.4.1/dist/mermaid.min.js';
      s.onload = () => resolve(window.mermaid); s.onerror = reject;
      document.head.appendChild(s);
    });
    mermaidLoad.then(mermaid => {
      const style = getComputedStyle(root);
      const v = name => style.getPropertyValue(name).trim();
      mermaid.initialize({
        startOnLoad: false, theme: 'base', fontFamily: 'Geist, system-ui, sans-serif',
        themeVariables: {
          primaryColor: v('--pc'), primaryTextColor: v('--on-pc'), primaryBorderColor: v('--primary'),
          lineColor: v('--text-3'), background: v('--pane'), mainBkg: v('--pc'), textColor: v('--text'),
        },
      });
      for (const b of blocks) {
        b.dataset.source ??= b.textContent;
        if (rerender || !b.hasAttribute('data-processed')) { b.removeAttribute('data-processed'); b.innerHTML = ''; b.textContent = b.dataset.source; }
      }
      mermaid.run({ nodes: blocks.filter(b => !b.hasAttribute('data-processed')) });
    }).catch(() => { });
  }

  /* ---------- Small utilities used by components ---------- */
  function focus(selector) { const el = document.querySelector(selector); el && el.focus(); }
  function scrollIntoView(selector) { const el = document.querySelector(selector); el && el.scrollIntoView({ block: 'nearest' }); }
  function setBodyLocked(locked) { root.classList.toggle('locked', !!locked); }

  return { getPreferences, setTheme, setAccent, registerShortcuts, initialDocument, activateHead, pageRendered, focus, scrollIntoView, setBodyLocked };
})();
