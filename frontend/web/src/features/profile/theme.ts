/** Applies the saved theme (Light / Dark / System) by toggling the `dark` class on <html>. */

export const THEME_STORAGE_KEY = "omni_theme";

/** Pure rule: Dark and Light are fixed, anything else (System, unknown, missing) follows the operating system. */
export function themeToDarkClass(theme: string | null | undefined, prefersDark: boolean): boolean {
  if (theme === "Dark") return true;
  if (theme === "Light") return false;
  return prefersDark;
}

/** Sets the class now and remembers the choice so the next page load can apply it before paint. */
export function applyTheme(theme: string | null | undefined): void {
  if (typeof document === "undefined") return;
  const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
  document.documentElement.classList.toggle("dark", themeToDarkClass(theme, prefersDark));
  try {
    if (theme) window.localStorage.setItem(THEME_STORAGE_KEY, theme);
  } catch {
    // Storage can be blocked; the class is already applied for this page.
  }
}

/** Inline script for the (app) layout: runs before paint, so a signed-in page does not flash the wrong theme. */
export const themeBootScript = `(function(){try{var t=localStorage.getItem("${THEME_STORAGE_KEY}");var d=t==="Dark"||(t!=="Light"&&window.matchMedia("(prefers-color-scheme: dark)").matches);document.documentElement.classList.toggle("dark",d);}catch(e){}})();`;
