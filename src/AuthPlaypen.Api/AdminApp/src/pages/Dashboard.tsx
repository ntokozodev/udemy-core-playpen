import { A } from "@solidjs/router";
import { Breadcrumbs } from "@/components/Breadcrumbs";

export function Dashboard() {
  const swaggerUrl = `${window.location.origin}/swagger`;

  return (
    <div class="space-y-8">
      <Breadcrumbs items={[{ label: "Home" }]} />
      <section class="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
        <p class="text-sm font-medium uppercase tracking-wide text-blue-600">Welcome</p>
        <h1 class="mt-2 text-3xl font-semibold text-slate-900">Admin Dashboard</h1>
        <p class="mt-4 text-slate-600">
          This panel helps you manage your authorization setup in one place. Start by creating applications,
          then connect them to scopes so each app only gets the permissions it needs.
        </p>
      </section>

      <section class="grid gap-4 md:grid-cols-2">
        <article class="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 class="text-xl font-semibold text-slate-900">System Description</h2>
          <p class="mt-3 text-sm leading-6 text-slate-600">
            The system is an OAuth/OpenID admin workspace where you define client applications and the scopes they
            can request. It gives your team a clear and centralized way to control who can access what.
          </p>
        </article>

        <article class="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 class="text-xl font-semibold text-slate-900">How It Works</h2>
          <ol class="mt-3 list-decimal space-y-2 pl-5 text-sm leading-6 text-slate-600">
            <li>Create an application with its client details.</li>
            <li>Create scopes that represent permissions or APIs.</li>
            <li>Assign scopes to applications to control access boundaries.</li>
          </ol>
        </article>
      </section>

      <section class="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <h2 class="text-xl font-semibold text-slate-900">Quick Links</h2>
        <p class="mt-2 text-sm text-slate-600">Jump directly to the sections below to continue setup.</p>
        <div class="mt-5 flex flex-wrap gap-3">
          <A
            class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-blue-700"
            href="/applications"
          >
            Go to Applications
          </A>
          <A
            class="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100"
            href="/scopes"
          >
            Go to Scopes
          </A>
          {!import.meta.env.PROD && (
            <a
              class="rounded-lg border border-emerald-300 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-700 transition hover:bg-emerald-100"
              href={swaggerUrl}
              rel="noreferrer"
              target="_blank"
            >
              Open API Docs
            </a>
          )}
        </div>
      </section>
    </div>
  );
}
