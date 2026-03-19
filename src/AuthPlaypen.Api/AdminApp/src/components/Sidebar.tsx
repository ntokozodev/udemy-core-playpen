import { A } from "@solidjs/router";
import type { Component } from "solid-js";
import { Show } from "solid-js";
import { isOidcAuthEnabled } from "@/services/authConfig";
import { signOut } from "@/services/authService";
import { AppIcon, ScopeIcon } from "@/layouts/Icons";

type SidebarProps = {
  collapsed?: boolean;
  onToggle?: () => void;
};

const activeClasses = "bg-brand-accent text-brand-dark font-semibold";

const Sidebar: Component<SidebarProps> = (props) => {
  const navItemClasses = () =>
    props.collapsed
      ? "flex h-12 items-center justify-center rounded-lg text-white transition-all duration-200 hover:bg-white/10"
      : "flex items-center gap-3 rounded-lg px-4 py-3 text-white transition-all duration-200 hover:bg-white/10";

  return (
    <aside
      class={`flex h-full flex-col bg-brand-dark p-6 transition-all duration-300 ${
        props.collapsed ? "w-20" : "w-64"
      }`}
    >
      <div class={`mb-5 ${props.collapsed ? "flex justify-center" : ""}`}>
        <div class={`flex items-center ${props.collapsed ? "justify-center" : "gap-3"}`}>
          <A href="/" end class={navItemClasses()} activeClass={activeClasses}>
            <div
              class={`flex items-center justify-center rounded-lg bg-brand-accent font-bold text-brand-dark transition-all duration-300 ${
                props.collapsed ? "h-8 w-8 text-xs" : "h-10 w-10 text-sm"
              }`}
            >
              GK
            </div>

            <Show when={!props.collapsed}>
              <span class="whitespace-nowrap text-lg font-semibold text-white">Gate Keeper</span>
            </Show>
          </A>
        </div>
      </div>

      <nav class="space-y-2">
        <A href="/applications" class={navItemClasses()} activeClass={activeClasses}>
          <AppIcon />
          <Show when={!props.collapsed}>
            <span>Applications</span>
          </Show>
        </A>

        <A href="/scopes" class={navItemClasses()} activeClass={activeClasses}>
          <ScopeIcon />
          <Show when={!props.collapsed}>
            <span>Scopes</span>
          </Show>
        </A>

        <Show when={isOidcAuthEnabled}>
          <button
            type="button"
            onClick={() => void signOut()}
            class={`w-full rounded-lg text-red-100 transition-all duration-200 hover:bg-white/10 hover:text-red-50 ${
              props.collapsed ? "flex h-12 items-center justify-center" : "px-4 py-3 text-left"
            }`}
          >
            <Show when={!props.collapsed} fallback={<span title="Sign out">⎋</span>}>
              <span>Sign out</span>
            </Show>
          </button>
        </Show>
      </nav>

      <div class="mt-auto pt-6">
        <button
          type="button"
          onClick={() => props.onToggle?.()}
          class={`w-full text-brand-accent hover:bg-white/10 rounded-lg transition ${
            props.collapsed ? "flex justify-center" : "flex items-center gap-3 px-4 py-3"
          }`}
        >
          <span class="text-lg">{props.collapsed ? "»" : "«"}</span>
          <Show when={!props.collapsed}>
            <span>Collapse</span>
          </Show>
        </button>
      </div>
    </aside>
  );
};

export default Sidebar;
