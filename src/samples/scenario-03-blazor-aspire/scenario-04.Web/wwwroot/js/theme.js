// ─── Theme management ───
const THEME_KEY = 'personaplex-theme';
const VALID_THEMES = ['system', 'light', 'dark'];

window.themeManager = {
    init() {
        const saved = localStorage.getItem(THEME_KEY) ?? 'system';
        this.apply(saved);
        return saved;
    },
    apply(theme) {
        if (!VALID_THEMES.includes(theme)) theme = 'system';
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);
    },
    get() {
        return document.documentElement.getAttribute('data-theme') ?? 'system';
    }
};
