/* This page may itself scroll on small windows. Each preview has its own constrained viewport;
   min-height/min-width: 0 prevent intrinsic content size from defeating flex/grid containment. */
.lab { flex: 1; min-height: 0; padding: 24px; display: flex; flex-direction: column; gap: 14px; overflow: auto; }
.settings { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }
.field { display: flex; flex-direction: column; gap: 6px; min-width: 0; }
.toggles, .commands { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; }
.hint { color: var(--nz-muted); }
.commands .hint { flex: 1; min-width: 200px; }
.previews { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; flex: 1; min-height: 250px; }
.pane { display: flex; flex-direction: column; gap: 8px; min-width: 0; min-height: 0; }
.pane-title { font-weight: 600; }
.offset { font-size: 12px; color: var(--nz-muted); }
.preview {
    flex: 1;
    min-width: 0;
    min-height: 0;
    overflow: auto;
    background: var(--nz-surface);
    border: 1px solid var(--nz-border);
    border-radius: 8px;
    /* 1.5 lines therefore means 36px per detent. This sets distance, never a row-alignment constraint. */
    line-height: 24px;
}

/* With no override, visibility and appearance inherit from App's Window through component boundaries.
   Visibility none suppresses scrollbar painting/hit testing while wheel and keyboard scrolling still work. */
.visibility-visible { scrollbar-visibility: visible; }
.visibility-autohide { scrollbar-visibility: autohide; }
.visibility-none { scrollbar-visibility: none; }
.placement-overlay { scrollbar-placement: overlay; }
.placement-reserve { scrollbar-placement: reserve; }
.smooth { scroll-behavior: smooth; }
.immediate { scroll-behavior: auto; }
.step-pixels { scroll-wheel-step: 24.5px; }
.step-lines { scroll-wheel-step: 1.5 lines; }
.step-system { scroll-wheel-step: auto; }
.step-zero { scroll-wheel-step: 0; }

/* This component-scoped skin overrides inherited geometry/colors on these two surfaces only.
   Track radius is independent of the thumb; hover/active/disabled colors describe the end buttons. */
.custom-appearance {
    scrollbar-width: 14px;
    scrollbar-inset: 4px;
    scrollbar-radius: 6px;
    scrollbar-min-thumb-length: 36px;
    scrollbar-opacity: 0.9;
    scrollbar-thumb-color: var(--nz-accent);
    scrollbar-track-color: #e5eafb;
    scrollbar-track-radius: 3px;
    scrollbar-track-border-width: 1px;
    scrollbar-track-border-color: #b2bfdc;
    scrollbar-button-length: 20px;
    scrollbar-button-color: #ffffff;
    scrollbar-button-background: #5269a5;
    scrollbar-button-hover-background: #3658ba;
    scrollbar-button-active-background: #233b83;
    scrollbar-button-disabled-color: #b5bed2;
    scrollbar-button-radius: 3px;
    transition: scrollbar-width 180ms ease-out, scrollbar-thumb-color 180ms ease-out;
}
.end-buttons {
    scrollbar-buttons: visible;
    scrollbar-button-step: 36px;
    scrollbar-button-repeat-delay: 400ms;
    scrollbar-button-repeat-interval: 75ms;
}
/* Zero disables held repetition while preserving one step per press. */
.no-repeat { scrollbar-button-repeat-interval: 0ms; }

/* Slow timing makes the automatic fade and the content motion easy to inspect independently.
   Reveal distance and wheel timeout control activity detection, not the content's scroll distance. */
.motion-slow {
    scrollbar-fade-in-duration: 350ms;
    scrollbar-fade-out-duration: 600ms;
    scrollbar-fade-out-delay: 700ms;
    scrollbar-fade-timing-function: cubic-bezier(.2, 0, .4, 1);
    scrollbar-reveal-distance: 32px;
    scrollbar-wheel-timeout: 900ms;
    scroll-duration: 550ms;
    scroll-timing-function: ease-in-out;
}
.motion-instant {
    scrollbar-fade-in-duration: 0ms;
    scrollbar-fade-out-duration: 0ms;
    scrollbar-fade-out-delay: 0ms;
    scroll-duration: 0ms;
}

/* A pseudo-class policy can suppress even the usual wheel reveal. It wins over proximity for its idle timeout. */
.hide-on-wheel:wheel-scrolling { scrollbar-visibility: none; }
/* A user animation changes only scrollbar properties; content opacity and layout remain unaffected. */
.wheel-animation:wheel-scrolling { animation: wheel-accent 600ms ease-out both; }
@keyframes wheel-accent {
    from { scrollbar-opacity: 0.3; scrollbar-thumb-color: #a65aa5; }
    to { scrollbar-opacity: 1; scrollbar-thumb-color: #3658ba; }
}
/* The runtime already bypasses smooth scrolling/fades for reduced motion. Disable this decorative animation too. */
@media (prefers-reduced-motion: reduce) {
    .wheel-animation:wheel-scrolling { animation: none; }
}
@media (max-width: 840px) {
    .settings { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .previews { grid-template-columns: minmax(0, 1fr); min-height: 560px; }
}
