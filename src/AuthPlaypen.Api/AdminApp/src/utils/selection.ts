export function mapSelectionToIds(selectedIds: string[], validIds: readonly string[]): string[] {
  const validIdSet = new Set(validIds);
  return Array.from(new Set(selectedIds.filter((id) => validIdSet.has(id))));
}
