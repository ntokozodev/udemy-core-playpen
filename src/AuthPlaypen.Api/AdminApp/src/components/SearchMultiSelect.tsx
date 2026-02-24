import { For } from "solid-js";

type Option = { id: string; label: string };

export function SearchMultiSelect(props: {
  label: string;
  searchTerm: string;
  onSearchTermChange: (value: string) => void;
  options: Option[];
  selected: string[];
  onChange: (ids: string[]) => void;
  placeholder?: string;
}) {
  const toggleValue = (id: string, checked: boolean) => {
    const currentValues = new Set(props.selected);
    if (checked) currentValues.add(id);
    else currentValues.delete(id);
    props.onChange(Array.from(currentValues));
  };

  return (
    <div class="space-y-2">
      <p class="text-sm text-slate-700">{props.label}</p>
      <input
        class="w-full rounded border p-2"
        placeholder={props.placeholder ?? `Search ${props.label.toLowerCase()}`}
        value={props.searchTerm}
        onInput={(e) => props.onSearchTermChange(e.currentTarget.value)}
      />
      <div class="max-h-56 space-y-1 overflow-y-auto rounded border border-slate-300 bg-white p-2">
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
    </div>
  );
}
