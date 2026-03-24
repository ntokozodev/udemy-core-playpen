import Sidebar from "@/components/Sidebar";
import type { RouteSectionProps } from "@solidjs/router";
import type { Component } from "solid-js";
import { createEffect, createSignal, onMount } from "solid-js";

type Theme = "light" | "dark";

const getPreferredTheme = (): Theme => {
  if (typeof window === "undefined") {
    return "light";
  }

  const storedTheme = window.localStorage.getItem("admin-theme");
  if (storedTheme === "light" || storedTheme === "dark") {
    return storedTheme;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
};

const applyTheme = (theme: Theme) => {
  document.documentElement.setAttribute("data-theme", theme);
};

export const MainLayout: Component<RouteSectionProps> = (props) => {
  const [mobileOpen, setMobileOpen] = createSignal(false);
  const [collapsed, setCollapsed] = createSignal(false);
  const [theme, setTheme] = createSignal<Theme>("light");

  onMount(() => {
    setTheme(getPreferredTheme());
  });

  createEffect(() => {
    const currentTheme = theme();
    applyTheme(currentTheme);
    window.localStorage.setItem("admin-theme", currentTheme);
  });

  const toggleTheme = () => {
    setTheme((currentTheme) => (currentTheme === "light" ? "dark" : "light"));
  };

  return (
    <div class="h-screen flex overflow-hidden bg-[var(--app-bg)] text-[var(--text-primary)] transition-colors duration-300">
      <div class="hidden md:block">
        <Sidebar collapsed={collapsed()} onToggle={() => setCollapsed(!collapsed())} />
      </div>

      <div class="relative flex-1 flex flex-col min-h-0 transition-all duration-300 bg-[var(--app-bg)]">
        <header class="md:hidden relative z-30 px-4 py-3 flex items-center justify-between bg-[var(--header-bg)] text-[var(--header-text)] transition-colors duration-300">
          <button
            type="button"
            onClick={() => setMobileOpen(true)}
            class="text-[var(--header-text)] text-xl"
            aria-label="Open navigation menu"
          >
            ☰
          </button>
          <span class="font-semibold">Admin Panel</span>
          <button
            type="button"
            onClick={toggleTheme}
            class="rounded-md px-2 py-1 text-sm border border-[var(--header-text)]/30 hover:bg-white/10 transition"
            aria-label={`Switch to ${theme() === "light" ? "dark" : "light"} theme`}
          >
            {theme() === "light" ? "🌙" : "☀️"}
          </button>
        </header>

        <main class="flex-1 overflow-y-auto p-6 md:p-10">
          <div class="mx-auto w-full max-w-4xl">
            <div class="hidden md:flex justify-end mb-4">
              <button
                type="button"
                onClick={toggleTheme}
                class="rounded-md border border-[var(--control-border)] px-3 py-2 text-sm text-[var(--text-primary)] bg-[var(--panel-bg)] hover:bg-[var(--panel-bg-hover)] transition-colors duration-200"
              >
                {theme() === "light" ? "Switch to dark mode" : "Switch to light mode"}
              </button>
            </div>
            {props.children}
          </div>
        </main>
      </div>

      {mobileOpen() && (
        <div class="fixed inset-0 bg-black/40 z-40 md:hidden" onClick={() => setMobileOpen(false)} />
      )}

      <div
        class={`fixed top-0 left-0 h-full w-64 bg-brand-dark z-50
          transform transition-transform duration-300 md:hidden
          ${mobileOpen() ? "translate-x-0" : "-translate-x-full"}`}
      >
        <Sidebar onToggle={() => setMobileOpen(false)} />
      </div>
    </div>
  );
};
