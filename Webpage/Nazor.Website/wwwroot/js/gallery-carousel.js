let prerenderedSelection;

function startCarousel(carousel) {
    const prerendered = document.querySelector('#app > .boot') !== null;
    const stage = carousel.querySelector('.gallery-stage');
    const artwork = stage.querySelector('.gallery-artwork');
    const lightScene = artwork.querySelector('.gallery-scene');
    const darkScene = lightScene.cloneNode(true);
    darkScene.classList.add('dark');
    darkScene.querySelectorAll('img[src]').forEach(image => image.removeAttribute('src'));
    artwork.append(darkScene);
    const desktops = [...lightScene.querySelectorAll('.gallery-desktop')].map((desktop, index) => [desktop, darkScene.children[index]]);
    const allDesktops = desktops.flat();
    const comparisonCaptures = [
        lightScene.querySelector('.gallery-window .gallery-capture:not(.dark)'),
        darkScene.querySelector('.gallery-window .gallery-capture.dark')
    ];
    const loads = [];
    const platformButtons = [...carousel.querySelectorAll('[data-gallery-platform]')];
    const status = stage.querySelector('.gallery-status');
    const statusText = status.querySelector('span:last-child');
    const themeSlider = stage.querySelector('.gallery-theme-slider');
    const restoredSelection = prerenderedSelection;
    prerenderedSelection = undefined;
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
    ].map(options => new Intl.DateTimeFormat('en-US', options));
    const clocks = desktops.flatMap((pair, index) => pair.map(desktop => ({
        element: desktop.querySelector('.gallery-clock'),
        time: desktop.querySelector('.gallery-clock-time'),
        date: desktop.querySelector('.gallery-clock-date'),
        dateFormat: dateFormats[index]
    })));
    const timeFormat = new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' });
    let active = platformButtons.findIndex(button => button.dataset.galleryPlatform === stage.dataset.platform);
    let transitioning = false;
    let inView = false;
    let ready = false;
    let loading = false;
    let selected = active;
    let selectionRequest = 0;
    let autoplayRequest = 0;
    let draggedPointer;
    let autoplayTimer;
    let slideTimer;
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

        clearTimeout(slideTimer);
        const outgoing = desktops[active];
        const incoming = desktops[next];
        for (const desktop of allDesktops) {
            desktop.classList.remove('arriving', 'departing');
            desktop.hidden = !incoming.includes(desktop) && !outgoing.includes(desktop);
        }

        active = next;
        stage.dataset.direction = direction < 0 ? 'backward' : 'forward';
        stage.dataset.platform = platformButtons[next].dataset.galleryPlatform;
        artwork.setAttribute('aria-label', 'The same Nazor app on the ' + platformButtons[next].textContent + ' desktop, in the dark and light color schemes');
        platformButtons.forEach((button, index) => button.setAttribute('aria-pressed', index === next));

        if (!direction || reducedMotion.matches) {
            finishSlide();
            return;
        }

        transitioning = true;
        outgoing.forEach(desktop => desktop.classList.add('departing'));
        incoming.forEach(desktop => desktop.classList.add('arriving'));
        slideTimer = setTimeout(finishSlide, slideDuration + 100);
    }

    function settleDesktops() {
        for (const desktop of allDesktops) {
            desktop.hidden = !desktops[active].includes(desktop);
            desktop.classList.remove('arriving', 'departing');
        }
    }

    function finishSlide() {
        clearTimeout(slideTimer);
        transitioning = false;
        settleDesktops();
        scheduleNext();
    }

    function scheduleNext() {
        clearTimeout(autoplayTimer);
        const request = ++autoplayRequest;
        if (!ready || loading || transitioning || !inView || document.hidden || reducedMotion.matches) return;

        const next = (active + 1) % desktops.length;
        const pending = loadDesktop(next);
        autoplayTimer = setTimeout(async () => {
            if (await pending && request === autoplayRequest && !listeners.signal.aborted) showDesktop(next, 1);
        }, desktopDuration);
    }

    function loadDesktop(index) {
        if (loads[index]) return loads[index];

        const [light, dark] = desktops[index];
        const captures = [...comparisonCaptures, light.querySelector('.gallery-capture:not(.dark)'), dark.querySelector('.gallery-capture.dark')];
        loads[index] = Promise.all(captures.map(capture => {
            capture.loading = 'eager';
            const source = capture.dataset.src ?? capture.getAttribute('src');
            if (!capture.hasAttribute('src') || capture.complete && !capture.naturalWidth) capture.src = source;
            return capture.decode();
        })).then(() => true, () => {
            loads[index] = undefined;
            return false;
        });
        return loads[index];
    }

    async function selectDesktop(index) {
        const request = ++selectionRequest;
        const platform = platformButtons[index].textContent;
        selected = index;
        loading = true;
        scheduleNext();
        artwork.setAttribute('aria-busy', 'true');
        status.removeAttribute('data-error');
        status.hidden = false;
        statusText.textContent = 'Loading ' + platform + ' preview…';

        const loaded = await loadDesktop(index);
        if (listeners.signal.aborted || request !== selectionRequest) return;

        loading = false;
        artwork.removeAttribute('aria-busy');
        if (!loaded) {
            status.dataset.error = '';
            statusText.textContent = 'Could not load the ' + platform + ' preview. Select the desktop to retry.';
            return;
        }

        status.hidden = true;
        const direction = ready ? Math.sign(index - active) : 0;
        ready = true;
        stage.dataset.ready = '';
        if (index !== active) showDesktop(index, direction);
        else scheduleNext();
    }

    platformButtons.forEach((button, index) => {
        button.disabled = false;
        button.addEventListener('click', () => selectDesktop(index), events);
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
        if (inView && !ready && !loading && !status.hasAttribute('data-error')) selectDesktop(active);
        else scheduleNext();
    }, { threshold: .15 });
    observer.observe(stage);
    if (restoredSelection) {
        themeSlider.valueAsNumber = restoredSelection.split;
        selectDesktop(restoredSelection.index);
    }
    updateComparison();
    updateClocks();

    return () => {
        if (prerendered && (ready || loading)) {
            prerenderedSelection = { index: loading ? selected : active, split: themeSlider.valueAsNumber };
        }
        observer.disconnect();
        listeners.abort();
        clearTimeout(autoplayTimer);
        clearTimeout(slideTimer);
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
