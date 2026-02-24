import { For, Show, createMemo, createSignal } from "solid-js";

type Option = { id: string; label: string };

export function SearchMultiSelect(props: {
  label: string;
  searchTerm: string;
  onSearchTermChange: (value: string) => void;
  options: Option[];
  selected: string[];
  onChange: (ids: string[]) => void;
  placeholder?: string;
  initialVisibleCount?: number;
}) {
  const [open, setOpen] = createSignal(false);

  const toggleValue = (id: string, checked: boolean) => {
    const currentValues = new Set(props.selected);
    if (checked) currentValues.add(id);
    else currentValues.delete(id);
    props.onChange(Array.from(currentValues));
  };

  const selectedLabel = createMemo(() => {
    if (props.selected.length === 0) {
      return props.label;
    }

    return `${props.label}: ${props.selected.length}`;
  });

  const filteredOptions = createMemo(() => {
    const term = props.searchTerm.trim().toLowerCase();

    if (!term) {
      return props.options.slice(0, props.initialVisibleCount ?? 6);
    }

    return props.options.filter((option) => option.label.toLowerCase().includes(term));
  });

  return (
    <div class="space-y-2">
      <p class="text-sm text-slate-700">{props.label}</p>
      <button
        class="flex w-full items-center justify-between rounded border border-slate-300 bg-white px-3 py-2 text-left text-sm"
        onClick={() => setOpen(!open())}
        type="button"
      >
        <span class="truncate">{selectedLabel()}</span>
        <span aria-hidden="true">▾</span>
      </button>
      <Show when={open()}>
        <div class="space-y-2 rounded border border-slate-300 bg-white p-2">
          <input
            class="w-full rounded border p-2"
            placeholder={props.placeholder ?? `Search ${props.label.toLowerCase()}`}
            value={props.searchTerm}
            onInput={(e) => props.onSearchTermChange(e.currentTarget.value)}
          />
          <div class="max-h-56 space-y-1 overflow-y-auto rounded border border-slate-200 bg-white p-2">
            <For each={filteredOptions()}>
              {(option) => (
                <label class="flex cursor-pointer items-center gap-2 rounded px-1 py-1 text-sm text-slate-700 hover:bg-slate-50">
                  <input
                    type="checkbox"
                    checked={props.selected.includes(option.id)}
                    onChange={(event) => toggleValue(option.id, event.currentTarget.checked)}
                  />
                  <span>{option.label}</span>
                </label>
              )}
            </For>
            <Show when={filteredOptions().length === 0}>
              <p class="px-1 py-2 text-sm text-slate-500">No matches found.</p>
            </Show>
          </div>
        </div>
      </Show>
    </div>
  );
}
