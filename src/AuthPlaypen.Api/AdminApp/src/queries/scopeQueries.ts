import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/solid-query";
import { scopeService } from "@/services/scopeService";

const FULL_LIST_PAGE_SIZE = 100;

export const scopeKeys = {
  all: ["scopes"] as const,
  paged: (pageSize: number) => [...scopeKeys.all, "paged", pageSize] as const,
  detail: (id: string) => [...scopeKeys.all, "detail", id] as const,
  search: (term: string) => [...scopeKeys.all, "search", term] as const,
};

export function useScopes() {
  return useInfiniteQuery(() => ({
    queryKey: scopeKeys.paged(FULL_LIST_PAGE_SIZE),
    queryFn: ({ pageParam }) => scopeService.getPaged(pageParam, FULL_LIST_PAGE_SIZE),
    getNextPageParam: (lastPage) => lastPage.nextCursor,
    initialPageParam: undefined as string | undefined,
    select: (data) => data.pages.flatMap((page) => page.items),
  }));
}

export function useScopeById(id: () => string | undefined) {
  return useQuery(() => ({
    queryKey: scopeKeys.detail(id() ?? ""),
    queryFn: () => scopeService.getById(id()!),
    enabled: Boolean(id()),
  }));
}

export function useSearchScopes(term: () => string, enabled: () => boolean) {
  return useQuery(() => ({
    queryKey: scopeKeys.search(term()),
    queryFn: () => scopeService.search(term()),
    enabled: enabled() && term().trim().length > 1,
  }));
}

export function useCreateScope() {
  const queryClient = useQueryClient();
  return useMutation(() => ({
    mutationFn: scopeService.create,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scopeKeys.all }),
  }));
}

export function useUpdateScope() {
  const queryClient = useQueryClient();
  return useMutation(() => ({
    mutationFn: ({ id, payload }: { id: string; payload: Parameters<typeof scopeService.update>[1] }) =>
      scopeService.update(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scopeKeys.all }),
  }));
}

export function useDeleteScope() {
  const queryClient = useQueryClient();
  return useMutation(() => ({
    mutationFn: scopeService.delete,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: scopeKeys.all }),
  }));
}
