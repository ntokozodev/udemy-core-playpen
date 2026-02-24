import { A } from "@solidjs/router";
import { createSignal } from "solid-js";

export function MainLayout(props: { children: unknown }) {
  const [collapsed, setCollapsed] = createSignal(false);

  return (
    <div class="flex min-h-screen bg-white text-slate-900">
      <aside class={`bg-slate-900 text-white transition-all ${collapsed() ? "w-16" : "w-64"}`}>
        <button class="m-2 rounded bg-blue-600 px-3 py-2 text-sm" onClick={() => setCollapsed((v) => !v)}>
          {collapsed() ? ">" : "<"}
        </button>
        <nav class="flex flex-col gap-2 p-2">
          <A class="rounded px-3 py-2 hover:bg-slate-700" href="/">Home</A>
          <A class="rounded px-3 py-2 hover:bg-slate-700" href="/applications">Applications</A>
          <A class="rounded px-3 py-2 hover:bg-slate-700" href="/scopes">Scopes</A>
        </nav>
      </aside>
      <main class="flex-1 bg-slate-50 p-6">
        <div class="mx-auto w-full max-w-4xl">
          {props.children}
        </div>
      </main>
    </div>
  );
}
