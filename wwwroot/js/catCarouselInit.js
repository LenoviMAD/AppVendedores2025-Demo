window.catCarouselInit = window.catCarouselInit || (function () {
    const timers = {};

    function clearTimer(carouselId) {
        if (timers[carouselId]) {
            clearInterval(timers[carouselId]);
            timers[carouselId] = null;
            delete timers[carouselId];
        }
    }

    function stop(carouselId) {
        try {
            clearTimer(carouselId);

            const el = document.getElementById(carouselId);
            if (!el || !window.bootstrap || !bootstrap.Carousel) return false;

            const existing = bootstrap.Carousel.getInstance(el);
            if (existing) {
                existing.dispose();
            }

            return true;
        } catch (err) {
            console.error("[catCarouselInit] stop ERROR:", err);
            return false;
        }
    }

    function start(carouselId, interval) {
        try {
            if (!carouselId) return false;

            const el = document.getElementById(carouselId);
            if (!el) return false;

            if (!window.bootstrap || !bootstrap.Carousel) {
                console.warn("[catCarouselInit] Bootstrap Carousel no encontrado.");
                return false;
            }

            const items = el.querySelectorAll(".carousel-item");
            if (!items || items.length <= 1) {
                return true; // si hay 0 o 1 slide, no hace falta autoplay
            }

            const intervalVal =
                interval ||
                parseInt(el.getAttribute("data-bs-interval")) ||
                2600;

            clearTimer(carouselId);

            const existing = bootstrap.Carousel.getInstance(el);
            if (existing) {
                existing.dispose();
            }

            items.forEach((x, i) => {
                if (i === 0) x.classList.add("active");
                else x.classList.remove("active");
            });

            const instance = new bootstrap.Carousel(el, {
                interval: false,   // importante: desactivar autoplay interno
                touch: true,
                pause: false,
                wrap: true
            });

            // pequeño delay extra para iPhone / WKWebView
            setTimeout(() => {
                clearTimer(carouselId);

                timers[carouselId] = setInterval(() => {
                    try {
                        const currentEl = document.getElementById(carouselId);
                        if (!currentEl) {
                            clearTimer(carouselId);
                            return;
                        }

                        // si está oculto, no mover
                        if (document.hidden) return;

                        const currentInstance = bootstrap.Carousel.getInstance(currentEl);
                        if (currentInstance) {
                            currentInstance.next();
                        }
                    } catch (e) {
                        console.error("[catCarouselInit] interval next ERROR:", e);
                    }
                }, intervalVal);
            }, 400);

            return true;
        } catch (err) {
            console.error("[catCarouselInit] start ERROR:", err);
            return false;
        }
    }

    function restart(carouselId, interval) {
        stop(carouselId);
        return start(carouselId, interval);
    }

    function exists() {
        return !!(window.bootstrap && bootstrap.Carousel);
    }

    // Cuando iPhone vuelve a activar la vista
    document.addEventListener("visibilitychange", () => {
        // no hacemos nada global acá; sólo evitamos mover carruseles ocultos
    });

    window.addEventListener("pageshow", () => {
        // ayuda en Safari/iPhone cuando vuelve desde background
    });

    return {
        start,
        stop,
        restart,
        exists
    };
})();