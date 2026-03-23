import Sidebar from "@/components/Sidebar";
import type { RouteSectionProps } from "@solidjs/router";
import type { Component } from "solid-js";
import { createSignal } from "solid-js";

export const MainLayout: Component<RouteSectionProps> = (props) => {
  const [mobileOpen, setMobileOpen] = createSignal(false);
  const [collapsed, setCollapsed] = createSignal(false);

  return (
    <div class="h-screen flex overflow-hidden">
      <div class="hidden md:block">
        <Sidebar collapsed={collapsed()} onToggle={() => setCollapsed(!collapsed())} />
      </div>

      <div class="relative flex-1 flex flex-col bg-gray-100 min-h-0 transition-all duration-300">
        <header class="md:hidden relative z-30 bg-brand-dark text-white px-4 py-3 flex items-center justify-between">
          <button
            type="button"
            onClick={() => setMobileOpen(true)}
            class="text-brand-accent text-xl"
          >
            ☰
          </button>
          <span class="font-semibold">Admin Panel</span>
          <div />
        </header>

        <main class="flex-1 overflow-y-auto p-6 md:p-10">
          <div class="mx-auto w-full max-w-4xl">{props.children}</div>
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
