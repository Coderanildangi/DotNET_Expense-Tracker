// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
    const THEME_KEY = "theme";
    const DARK_THEME_URL = "https://cdn.syncfusion.com/ej2/20.1.55/bootstrap5-dark.css";
    const LIGHT_THEME_URL = "https://cdn.syncfusion.com/ej2/20.1.55/bootstrap5.css";

    const getPreferredTheme = () => {
        try {
            const savedTheme = localStorage.getItem(THEME_KEY);
            if (savedTheme === "dark" || savedTheme === "light") {
                return savedTheme;
            }
        } catch (error) {
            // Ignore storage access errors and fall back.
        }
        return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    };

    const applyTheme = (theme) => {
        document.documentElement.setAttribute("data-theme", theme);

        const syncfusionTheme = document.getElementById("syncfusion-theme");
        if (syncfusionTheme) {
            syncfusionTheme.setAttribute("href", theme === "dark" ? DARK_THEME_URL : LIGHT_THEME_URL);
        }

        const toggleIcon = document.getElementById("theme-toggle-icon");
        const toggleText = document.getElementById("theme-toggle-text");
        if (toggleIcon) {
            toggleIcon.className = theme === "dark" ? "fa-solid fa-sun" : "fa-solid fa-moon";
        }
        if (toggleText) {
            toggleText.textContent = theme === "dark" ? "Light" : "Dark";
        }
    };

    document.addEventListener("DOMContentLoaded", () => {
        const initialTheme = getPreferredTheme();
        applyTheme(initialTheme);

        const themeToggleButton = document.getElementById("theme-toggle");
        if (!themeToggleButton) {
            return;
        }

        themeToggleButton.addEventListener("click", () => {
            const currentTheme = document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
            const nextTheme = currentTheme === "dark" ? "light" : "dark";
            applyTheme(nextTheme);
            try {
                localStorage.setItem(THEME_KEY, nextTheme);
            } catch (error) {
                // Ignore storage access errors.
            }
        });
    });
})();
