function startCarousel(carousel) {
    const stage = carousel.querySelector('.gallery-stage');
    const artwork = stage.querySelector('.gallery-artwork');
    const lightScene = artwork.querySelector('.gallery-scene');
    const darkScene = lightScene.cloneNode(true);
    darkScene.classList.add('dark');
    artwork.append(darkScene);
    const desktops = [...lightScene.querySelectorAll('.gallery-desktop')].map((desktop, index) => [desktop, darkScene.children[index]]);
    const allDesktops = desktops.flat();
    const platformButtons = [...carousel.querySelectorAll('[data-gallery-platform]')];
    const themeSlider = stage.querySelector('.gallery-theme-slider');
    const compareHandle = stage.querySelector('.gallery-compare-handle');
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
    const slideDuration = parseFloat(getComputedStyle(stage).getPropertyValue('--slide-duration'));
    const desktopDuration = 1500;
    const listeners = new AbortController();
    const events = { signal: listeners.signal };
    const dateFormats = [
        { day: 'numeric', month: 'numeric', year: 'numeric' },
        { day: 'numeric', month: 'numeric', year: '2-digit' },
        { day: 'numeric', month: 'short' }
    ].map(options => new Intl.DateTimeFormat(undefined, options));
    const clocks = desktops.flatMap((pair, index) => pair.map(desktop => ({
        element: desktop.querySelector('.gallery-clock'),
        time: desktop.querySelector('.gallery-clock-time'),
        date: desktop.querySelector('.gallery-clock-date'),
        dateFormat: dateFormats[index]
    })));
    const timeFormat = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' });
    let active = platformButtons.findIndex(button => button.dataset.galleryPlatform === stage.dataset.platform);
    let transitioning = false;
    let inView = false;
    let capturesReady;
    let assetsReady = false;
    let selectionRequest = 0;
    let draggedPointer;
    let timer;
    let clockTimer;

    function updateComparison() {
        const split = themeSlider.valueAsNumber;
        stage.style.setProperty('--split', split + '%');
        themeSlider.setAttribute('aria-valuetext', Math.round(split) + '% dark');
    }

    function moveComparison(event) {
        const { left, width } = stage.getBoundingClientRect();
        const split = (event.clientX - left) / width * 100;
        themeSlider.valueAsNumber = Math.max(0, Math.min(100, split));
        updateComparison();
    }

    function updateClocks() {
        clearTimeout(clockTimer);
        const now = new Date();
        const time = timeFormat.format(now);
        for (const clock of clocks) {
            clock.element.dateTime = now.toISOString();
            clock.time.textContent = time;
            clock.date.textContent = clock.dateFormat.format(now);
        }
        clockTimer = setTimeout(updateClocks, 60_000 - Date.now() % 60_000);
    }

    function showDesktop(next, direction) {
        if (next === active) return;

        clearTimeout(timer);
        const outgoing = desktops[active];
        const incoming = desktops[next];
        for (const desktop of allDesktops) {
            desktop.classList.remove('arriving', 'departing');
            desktop.hidden = !incoming.includes(desktop) && !outgoing.includes(desktop);
        }

        active = next;
        stage.dataset.direction = direction < 0 ? 'backward' : 'forward';
        stage.dataset.platform = platformButtons[next].dataset.galleryPlatform;
        artwork.setAttribute('aria-label', 'Nazor Gallery on the ' + platformButtons[next].textContent + ' desktop in dark and light themes');
        platformButtons.forEach((button, index) => button.setAttribute('aria-pressed', index === next));

        if (reducedMotion.matches) {
            finishSlide();
            return;
        }

        transitioning = true;
        outgoing.forEach(desktop => desktop.classList.add('departing'));
        incoming.forEach(desktop => desktop.classList.add('arriving'));
        timer = setTimeout(finishSlide, slideDuration + 100);
    }

    function settleDesktops() {
        for (const desktop of allDesktops) {
            desktop.hidden = !desktops[active].includes(desktop);
            desktop.classList.remove('arriving', 'departing');
        }
    }

    function finishSlide() {
        transitioning = false;
        settleDesktops();
        scheduleNext();
    }

    function scheduleNext() {
        if (transitioning) return;
        clearTimeout(timer);
        if (!assetsReady || !inView || document.hidden || reducedMotion.matches) return;
        timer = setTimeout(() => showDesktop((active + 1) % desktops.length, 1), desktopDuration);
    }

    function loadCaptures() {
        if (!capturesReady) {
            const captures = stage.querySelectorAll('.gallery-scene:not(.dark) .gallery-capture:not(.dark), .gallery-scene.dark .gallery-capture.dark');
            capturesReady = Promise.allSettled([...captures].map(capture => {
                capture.loading = 'eager';
                return capture.decode?.();
            })).then(() => {
                if (listeners.signal.aborted) return;

                assetsReady = true;
                scheduleNext();
            });
        }
        return capturesReady;
    }

    platformButtons.forEach((button, index) => {
        button.addEventListener('click', async () => {
            const request = ++selectionRequest;
            await loadCaptures();
            if (listeners.signal.aborted || request !== selectionRequest) return;

            showDesktop(index, index > active ? 1 : -1);
        }, events);
    });
    stage.addEventListener('animationend', event => {
        if (transitioning && event.target === desktops[active][0]) finishSlide();
    }, events);
    themeSlider.addEventListener('input', updateComparison, events);
    compareHandle.addEventListener('pointerdown', event => {
        if (!event.isPrimary || event.button !== 0) return;

        event.preventDefault();
        themeSlider.focus({ preventScroll: true });
        draggedPointer = event.pointerId;
        compareHandle.setPointerCapture(event.pointerId);
        moveComparison(event);
    }, events);
    window.addEventListener('pointermove', event => {
        if (event.pointerId === draggedPointer && event.buttons) moveComparison(event);
    }, events);
    window.addEventListener('pointerup', event => {
        if (event.pointerId !== draggedPointer) return;

        moveComparison(event);
        draggedPointer = undefined;
    }, events);
    window.addEventListener('pointercancel', event => {
        if (event.pointerId === draggedPointer) draggedPointer = undefined;
    }, events);
    window.addEventListener('blur', () => { draggedPointer = undefined; }, events);
    reducedMotion.addEventListener('change', () => {
        if (reducedMotion.matches && transitioning) finishSlide();
        else scheduleNext();
    }, events);
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) updateClocks();
        scheduleNext();
    }, events);

    const observer = new IntersectionObserver(([entry]) => {
        inView = entry.intersectionRatio >= .15;
        if (inView) loadCaptures();
        scheduleNext();
    }, { threshold: .15 });
    observer.observe(stage);
    updateComparison();
    updateClocks();

    return () => {
        observer.disconnect();
        listeners.abort();
        clearTimeout(timer);
        clearTimeout(clockTimer);
        settleDesktops();
        darkScene.remove();
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
