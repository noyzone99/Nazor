.demo-root { display: flex; flex-direction: column; flex: 1; min-height: 0; }
:root[role=window].gallery-leaf {
    --gallery-pane: var(--leaf-pane);
    --gallery-depth: var(--leaf-sh-pop), inset 0 0 0 1px var(--leaf-stroke);
    background-color: var(--leaf-tray);
    background-image: none;
}
:root.seed-green { --leaf-primary: #146c2e; --leaf-on-primary: #ffffff; --leaf-pc: #c4eed0; --leaf-on-pc: #072711; --leaf-ink: #146c2e; }
:root.seed-red { --leaf-primary: #c5221f; --leaf-on-primary: #ffffff; --leaf-pc: #ffdad6; --leaf-on-pc: #410e0b; --leaf-ink: #b3261e; }
:root.seed-yellow { --leaf-primary: #f9ab00; --leaf-on-primary: #2a1e00; --leaf-pc: #ffe9b0; --leaf-on-pc: #2a1e00; --leaf-ink: #8a5a00; }
@media (prefers-color-scheme: dark) {
    :root.seed-green { --leaf-primary: #6dd58c; --leaf-on-primary: #0a3818; --leaf-pc: #0f5223; --leaf-on-pc: #c4eed0; --leaf-ink: #6dd58c; }
    :root.seed-red { --leaf-primary: #ffb4ab; --leaf-on-primary: #690005; --leaf-pc: #93000a; --leaf-on-pc: #ffdad6; --leaf-ink: #ffb4ab; }
    :root.seed-yellow { --leaf-primary: #fdd663; --leaf-on-primary: #3d2e00; --leaf-pc: #5c4400; --leaf-on-pc: #ffe08a; --leaf-ink: #fdd663; }
}
.demo-options { display: flex; flex-direction: row; align-items: center; flex-wrap: wrap; gap: 14px; padding: 8px 20px; }
.demo-search { width: 310px; }
.demo-tsearch { position: relative; width: 300px; max-width: 100%; }
.demo-tsearch > SearchField { width: 100%; min-height: 32px; padding-right: 60px; background: color-mix(in srgb, var(--leaf-pane) 60%, transparent); box-shadow: inset 0 0 0 1px var(--leaf-stroke); font-size: 13px; }
.demo-tsearch > Kbd { position: absolute; right: 8px; top: 7px; padding: 2px 6px; border-radius: var(--leaf-r-xs); background: transparent; box-shadow: inset 0 0 0 1px var(--leaf-stroke-2); color: var(--leaf-text-3); font-size: 11px; font-weight: 500; line-height: 1.2; pointer-events: none; }
/* Seed picker, as .seeds and .seedbtn: leaf swatches in a recessed leaf; the chosen one turns and wears a ring. */
.demo-seeds { display: flex; flex-direction: row; gap: 6px; margin-left: auto; padding: 5px; border-radius: 13px 4px 13px 4px; background: color-mix(in srgb, var(--leaf-s2) 85%, transparent); box-shadow: inset 0 0 0 1px var(--leaf-stroke); }
.demo-seed, .demo-seed:hover, .demo-seed:active, .demo-seed[pressed], .demo-seed[pressed]:hover { min-width: 24px; min-height: 24px; width: 24px; height: 24px; padding: 0; border-radius: 11px 3px 11px 3px; background: var(--demo-seed); box-shadow: none; transform: rotate(0deg); transition: transform 300ms cubic-bezier(0.34, 1.5, 0.64, 1), box-shadow 200ms; }
.demo-seed:hover { transform: rotate(-12deg) scale(1.08); }
.demo-seed[pressed], .demo-seed[pressed]:hover { box-shadow: 0 0 0 2px var(--leaf-pane), 0 0 0 4px var(--demo-seed); transform: rotate(90deg); }
.demo-shell { flex: 1; min-height: 0; display: grid; grid-template-columns: 88px minmax(0px, 1fr); grid-template-rows: minmax(0px, 1fr); }
.demo-rail { position: relative; min-height: 0; overflow-y: auto; display: flex; flex-direction: column; align-items: center; gap: 4px; padding: 6px 0 14px; }
.demo-rail-ind { position: absolute; left: var(--nz-indicator-x); top: 0; width: var(--nz-indicator-width); height: var(--nz-indicator-height); transform: translateY(var(--nz-indicator-y)); transition: transform 550ms cubic-bezier(0.16, 1, 0.3, 1); pointer-events: none; z-index: 0; }
.demo-rail-ind::before { content: ""; position: absolute; inset: 0; border-radius: 16px 5px 16px 5px; background: var(--leaf-pc); }
.demo-rail-ind.pop-a::before { animation: demo-rail-pop-a 550ms cubic-bezier(0.16, 1, 0.3, 1); }
.demo-rail-ind.pop-b::before { animation: demo-rail-pop-b 550ms cubic-bezier(0.16, 1, 0.3, 1); }
@keyframes demo-rail-pop-a { 30% { transform: scale(1.25, 0.8); } 60% { transform: scale(0.94, 1.06); } }
@keyframes demo-rail-pop-b { 30% { transform: scale(1.25, 0.8); } 60% { transform: scale(0.94, 1.06); } }
/* The navigation rail of the prototype: a 56 x 32 leaf slot for the icon, the label below; the chosen slot takes the
   primary container. */
.demo-nav { position: relative; z-index: 1; display: flex; flex-direction: column; align-items: center; width: 76px; min-height: 0; padding: 0 0 6px; border-radius: var(--leaf-r-ctl); background: transparent; box-shadow: none; color: var(--leaf-text-2); font-size: 11.5px; font-weight: 500; line-height: 1.2; }
.demo-nav:hover, .demo-nav:active { background: transparent; color: var(--leaf-text-2); transform: scale(1); border-radius: var(--leaf-r-ctl); }
.demo-nav ButtonLabel { display: flex; flex-direction: column; align-items: center; gap: 4px; }
.demo-slot { display: flex; align-items: center; justify-content: center; width: 56px; height: 32px; border-radius: 16px 5px 16px 5px; transition: background-color 200ms; }
.demo-nav:hover .demo-slot { background: var(--leaf-hover); }
.demo-nav.current, .demo-nav.current:hover { color: var(--leaf-text); }
.demo-nav.current .demo-slot { background: transparent; color: var(--leaf-on-pc); }
.demo-slot Icon { width: 20px; height: 20px; font-size: 20px; }

.demo-rail-avatar { --nz-avatar-size: 36px; font-size: 12px; background: var(--leaf-g1); color: #ffffff; }
.demo-rail-space { flex: 1; }
.demo-mnav { display: none; }
.demo-mnav-item { flex-shrink: 0; display: flex; align-items: center; gap: 6px; height: 34px; min-height: 34px; padding: 0 13px; border-radius: 12px 3px 12px 3px; color: var(--leaf-text-2); font-size: 13.5px; font-weight: 550; background: var(--leaf-s1); box-shadow: none; transition: background-color 250ms, color 200ms; }
.demo-mnav-item ButtonLabel { display: flex; flex-direction: row; align-items: center; gap: 6px; }
.demo-mnav-item Icon { width: 18px; height: 18px; font-size: 18px; }
.demo-mnav-item:hover, .demo-mnav-item:active { border-radius: 12px 3px 12px 3px; background: var(--leaf-s1); color: var(--leaf-text-2); box-shadow: none; transform: scale(1); }
.demo-mnav-item.current, .demo-mnav-item.current:hover, .demo-mnav-item.current:active { background: var(--leaf-pc); color: var(--leaf-on-pc); }
.demo-pane { display: flex; flex-direction: column; min-width: 0; min-height: 0; margin: 0 10px 10px 0; border-radius: var(--leaf-r-pane); background: var(--gallery-pane); box-shadow: var(--gallery-depth); overflow-x: hidden; overflow-y: hidden; }
.demo-page-head { display: flex; flex-direction: row; flex-shrink: 0; align-items: flex-end; gap: 12px; padding: 26px 36px 14px; transition: padding 350ms cubic-bezier(0.16, 1, 0.3, 1), box-shadow 300ms; }
.demo-page-head.compact { padding-top: 12px; padding-bottom: 10px; box-shadow: 0 1px 0 var(--leaf-stroke); }
.demo-page-head.compact .demo-page-title { font-size: 20px; }
.demo-page-head.compact .demo-page-sub { opacity: 0; }
.demo-page-head > Container:first-child { flex: 1; }
.demo-page-sub { margin-bottom: 4px; color: var(--leaf-text-3); font-size: 13px; transition: opacity 200ms; }
.demo-page-head .demo-page-title { font-size: 34px; font-weight: 800; line-height: 1.05; letter-spacing: -0.035em; transition: font-size 350ms cubic-bezier(0.16, 1, 0.3, 1); }
.demo-scroll { flex: 1; min-height: 0; }
.demo-page { display: flex; flex-direction: column; max-width: 980px; padding: 6px 36px 72px; }
/* The page layout mirrors the prototype's classes: section.sec, h3.st, .lead, .card, .wrap, .grid, .stack. */
.demo-section { display: flex; flex-direction: column; padding: 10px 0 40px; }
.demo-section > Heading[level="3"] { margin-bottom: 4px; font-size: 22px; font-weight: 700; line-height: 1.2; letter-spacing: -0.02em; }
.demo-secondary { color: var(--leaf-text-2); font-size: 13px; }
/* The lead follows the heading directly: the theme's prose rhythm (Heading + Paragraph) does not apply here, as in .sec > .lead. */
.demo-lead { margin-top: 0; margin-bottom: 20px; width: 610px; max-width: 100%; color: var(--leaf-text-2); line-height: 1.5; }
.demo-card { --leaf-slider-cutout: var(--leaf-s1); display: flex; flex-direction: column; min-width: 0; padding: 18px 20px 20px; border-radius: var(--leaf-r-ovl); background: var(--leaf-s1); }
.demo-card > Heading[level="4"] { margin-bottom: 14px; font-size: 13px; font-weight: 600; line-height: 1.5; color: var(--leaf-text-2); }
.demo-row { display: flex; flex-direction: row; align-items: center; flex-wrap: wrap; gap: 10px; }
.demo-row + .demo-row { margin-top: 12px; }
.demo-stack { display: flex; flex-direction: column; gap: 12px; }
.demo-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(270px, 1fr)); gap: 14px; }
.demo-section > .demo-grid + .demo-card, .demo-section > .demo-card + .demo-grid, .demo-section > .demo-card + .demo-card { margin-top: 14px; }
/* A colored leaf holding a list icon, as .licon. */
.demo-licon { display: flex; align-items: center; justify-content: center; width: 36px; height: 36px; border-radius: 13px 4px 13px 4px; background: var(--demo-c); color: #ffffff; }
.demo-licon Icon { width: 18px; height: 18px; font-size: 18px; }
.demo-hint { color: var(--leaf-text-3); font-size: 12.5px; }
/* The prototype keeps the floating button in the flow of its card (.card > .fab), not pinned to a corner. */
.demo-floating { position: relative; }
.demo-floating > FloatingActionButton { position: static; align-self: flex-start; }
.demo-data { height: 250px; }
.demo-list { height: 180px; }
.demo-brand-icon { width: 84px; height: 84px; display: flex; align-items: center; justify-content: center; border-radius: 28px 8px 28px 8px; background: var(--leaf-primary); }
.demo-brand-icon.inverse { background: var(--leaf-inverse); }
.demo-brand-icon LeafMark { --leaf-mk1: var(--leaf-on-primary); --leaf-mk2: color-mix(in srgb, var(--leaf-on-primary) 50%, var(--leaf-primary)); }
.demo-brand-icon.inverse LeafMark { --leaf-mk1: var(--leaf-on-inverse); --leaf-mk2: color-mix(in srgb, var(--leaf-on-inverse) 45%, var(--leaf-inverse)); }
@media (max-width: 860px) {
    .demo-shell { grid-template-columns: minmax(0px, 1fr); }
    .demo-rail { display: none; }
    .demo-pane { margin: 0 6px 6px; }
    .demo-page-head { padding: 22px 18px 6px; }
    .demo-page-head .demo-page-title { font-size: 28px; }
    .demo-page { padding: 0 16px 40px; }
    .demo-mnav { display: flex; flex-direction: row; flex-shrink: 0; gap: 6px; overflow-x: auto; overflow-y: hidden; scrollbar-width: 0px; padding: 10px 12px; background: var(--leaf-pane); border-bottom: 1px solid var(--leaf-stroke); scroll-behavior: smooth; scroll-duration: 120ms; }
}
@media (prefers-reduced-motion: reduce) { .demo-rail-ind, .demo-page-head, .demo-page-head .demo-page-title, .demo-page-sub { transition: none; } .demo-rail-ind::before { animation: none; } .demo-scroll, .demo-mnav { scroll-behavior: auto; } }
