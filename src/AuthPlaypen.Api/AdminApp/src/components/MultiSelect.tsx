import { For, createMemo } from "solid-js";

type Option = { id: string; label: string };

export function MultiSelect(props: {
  options: Option[];
  selected: string[];
  onChange: (ids: string[]) => void;
  label: string;
  placeholder?: string;
}) {
  const selectedLabel = createMemo(() => {
    if (props.selected.length === 0) {
      return props.placeholder ?? `Select ${props.label.toLowerCase()}`;
    }

    const selectedOptions = props.options.filter((option) => props.selected.includes(option.id));
    if (selectedOptions.length === 0) {
      return `${props.selected.length} selected`;
    }

    if (selectedOptions.length <= 2) {
      return selectedOptions.map((option) => option.label).join(", ");
    }

    return `${selectedOptions.length} selected`;
  });

  const toggleValue = (id: string, checked: boolean) => {
    const currentValues = new Set(props.selected);
    if (checked) {
      currentValues.add(id);
    } else {
      currentValues.delete(id);
    }

    props.onChange(Array.from(currentValues));
  };

  return (
    <div class="space-y-2">
      <p class="text-sm text-slate-700">{props.label}</p>
      <details class="group relative">
        <summary class="flex w-full cursor-pointer list-none items-center justify-between rounded border border-slate-300 bg-white p-2 text-sm text-slate-800 marker:hidden">
          <span class="truncate pr-4">{selectedLabel()}</span>
          <span class="text-slate-500 transition-transform group-open:rotate-180">▾</span>
        </summary>
        <div class="absolute z-10 mt-1 max-h-60 w-full overflow-y-auto rounded border border-slate-300 bg-white p-2 shadow-lg">
          <For each={props.options}>
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
        </div>
      </details>
    </div>
  );
}
