function startCarousel(carousel) {
    const stage = carousel.querySelector('.gallery-stage');
    const desktops = [...stage.querySelectorAll('.gallery-desktop')];
    const platformButtons = [...carousel.querySelectorAll('[data-gallery-platform]')];
    const playback = carousel.querySelector('.gallery-playback');
    const themeToggle = carousel.querySelector('.gallery-theme');
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
    const slideDuration = parseFloat(getComputedStyle(stage).getPropertyValue('--slide-duration'));
    const desktopDuration = 4600;
    const listeners = new AbortController();
    const events = { signal: listeners.signal };
    const clocks = desktops.map(desktop => ({
        element: desktop.querySelector('.gallery-clock'),
        time: desktop.querySelector('.gallery-clock-time'),
        date: desktop.querySelector('.gallery-clock-date')
    }));
    const dateFormats = [
        { day: 'numeric', month: 'numeric', year: 'numeric' },
        { day: 'numeric', month: 'numeric', year: '2-digit' },
        { day: 'numeric', month: 'short' }
    ].map(options => new Intl.DateTimeFormat(undefined, options));
    const timeFormat = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' });
    let active = platformButtons.findIndex(button => button.dataset.galleryPlatform === stage.dataset.platform);
    let playing = !reducedMotion.matches;
    let inView = false;
    let timer;
    let clockTimer;

    function updateClocks() {
        clearTimeout(clockTimer);
        const now = new Date();
        const time = timeFormat.format(now);
        for (const [index, clock] of clocks.entries()) {
            clock.element.dateTime = now.toISOString();
            clock.time.textContent = time;
            clock.date.textContent = dateFormats[index].format(now);
        }
        clockTimer = setTimeout(updateClocks, 60_000 - Date.now() % 60_000);
    }

    function showDesktop(next, direction) {
        if (next === active) return;

        const outgoing = desktops[active];
        const incoming = desktops[next];
        for (const desktop of desktops) {
            for (const animation of desktop.getAnimations()) {
                animation.onfinish = null;
                animation.cancel();
            }
            desktop.hidden = desktop !== outgoing && desktop !== incoming;
        }

        active = next;
        stage.dataset.platform = platformButtons[next].dataset.galleryPlatform;
        stage.setAttribute('aria-label', 'Nazor Gallery on the ' + platformButtons[next].textContent + ' desktop');
        platformButtons.forEach((button, index) => button.setAttribute('aria-pressed', index === next));

        if (reducedMotion.matches) {
            outgoing.hidden = true;
            return;
        }

        const timing = { duration: slideDuration, easing: 'cubic-bezier(.65, 0, .35, 1)', fill: 'both' };
        const slideOut = outgoing.animate({ transform: ['translateX(0)', `translateX(${-direction * 100}%)`] }, timing);
        const slideIn = incoming.animate({ transform: [`translateX(${direction * 100}%)`, 'translateX(0)'] }, timing);
        slideIn.onfinish = () => {
            outgoing.hidden = true;
            slideOut.cancel();
            slideIn.cancel();
        };
    }

    function scheduleNext() {
        clearTimeout(timer);
        if (!playing || !inView || document.hidden) return;
        timer = setTimeout(() => {
            showDesktop((active + 1) % desktops.length, 1);
            scheduleNext();
        }, desktopDuration);
    }

    function setPlaying(value) {
        playing = value;
        playback.textContent = playing ? 'Pause' : 'Play';
        scheduleNext();
    }

    function setTheme(theme) {
        stage.dataset.theme = theme;
        themeToggle.setAttribute('aria-pressed', theme === 'dark');
    }

    platformButtons.forEach((button, index) => {
        button.addEventListener('click', () => {
            showDesktop(index, index > active ? 1 : -1);
            scheduleNext();
        }, events);
    });
    playback.addEventListener('click', () => setPlaying(!playing), events);
    themeToggle.addEventListener('click', () => setTheme(stage.dataset.theme === 'light' ? 'dark' : 'light'), events);
    reducedMotion.addEventListener('change', event => {
        if (event.matches) setPlaying(false);
    }, events);
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) updateClocks();
        scheduleNext();
    }, events);

    const observer = new IntersectionObserver(([entry]) => {
        inView = entry.isIntersecting;
        if (inView) {
            for (const image of stage.querySelectorAll('img[loading="lazy"]')) image.loading = 'eager';
        }
        scheduleNext();
    }, { threshold: .15 });
    observer.observe(stage);
    const siteTheme = document.documentElement.dataset.theme;
    setTheme(siteTheme ?? (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'));
    setPlaying(playing);
    updateClocks();

    return () => {
        observer.disconnect();
        listeners.abort();
        clearTimeout(timer);
        clearTimeout(clockTimer);
    };
}

class GalleryCarousel extends HTMLElement {
    #stop;

    connectedCallback() {
        queueMicrotask(() => {
            if (this.isConnected && !this.#stop) this.#stop = startCarousel(this);
        });
    }

    disconnectedCallback() {
        this.#stop?.();
        this.#stop = undefined;
    }
}

customElements.define('nazor-gallery-carousel', GalleryCarousel);
