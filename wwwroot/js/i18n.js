// wwwroot/js/i18n.js
window.i18n = (function () {
    function setTexts(dict) {
        try {
            if (!dict || typeof dict !== "object") return;
            const nodes = document.querySelectorAll("[data-i18n]");
            nodes.forEach(n => {
                const key = n.getAttribute("data-i18n");
                if (!key) return;
                const v = dict[key];
                if (v == null) return; // clave no enviada
                n.innerHTML = v; // usa innerHTML porque tenés <strong> en valores
            });
        } catch (e) {
            console.error("[i18n] setTexts error:", e);
        }
    }
    return { setTexts };
})();

