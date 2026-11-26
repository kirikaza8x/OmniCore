"use client";

import { useEffect } from "react";
import { applyTheme } from "../theme";
import { useProfile } from "../useProfile";

/** Applies the signed-in user's saved theme; with "System" it follows the OS and reacts when the OS changes. */
export function ThemeApplier() {
  const { profile } = useProfile();
  const theme = profile?.settings?.theme;

  useEffect(() => {
    if (!theme) return;
    applyTheme(theme);
    if (theme !== "System") return;
    const query = window.matchMedia("(prefers-color-scheme: dark)");
    const onChange = () => applyTheme("System");
    query.addEventListener("change", onChange);
    return () => query.removeEventListener("change", onChange);
  }, [theme]);

  return null;
}
