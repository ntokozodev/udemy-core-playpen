import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/solid-query";
import { applicationService } from "@/services/applicationService";

const FULL_LIST_PAGE_SIZE = 100;

export const applicationKeys = {
  all: ["applications"] as const,
  paged: (pageSize: number) => [...applicationKeys.all, "paged", pageSize] as const,
  detail: (id: string) => [...applicationKeys.all, "detail", id] as const,
  search: (term: string) => [...applicationKeys.all, "search", term] as const,
};

export function useApplications() {
  return useInfiniteQuery(() => ({
    queryKey: applicationKeys.paged(FULL_LIST_PAGE_SIZE),
    queryFn: ({ pageParam }) => applicationService.getPaged(pageParam, FULL_LIST_PAGE_SIZE),
    getNextPageParam: (lastPage) => lastPage.nextCursor,
    initialPageParam: undefined as string | undefined,
    select: (data) => data.pages.flatMap((page) => page.items),
  }));
}

export function useApplicationById(id: () => string | undefined) {
  return useQuery(() => ({
    queryKey: applicationKeys.detail(id() ?? ""),
    queryFn: () => applicationService.getById(id()!),
    enabled: Boolean(id()),
  }));
}

export function useSearchApplications(term: () => string, enabled: () => boolean) {
  return useQuery(() => ({
    queryKey: applicationKeys.search(term()),
    queryFn: () => applicationService.search(term()),
    enabled: enabled() && term().trim().length > 1,
  }));
}

export function useCreateApplication() {
  const queryClient = useQueryClient();
  return useMutation(() => ({
    mutationFn: applicationService.create,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: applicationKeys.all }),
  }));
}

export function useUpdateApplication() {
  const queryClient = useQueryClient();
  return useMutation(() => ({
    mutationFn: ({ id, payload }: { id: string; payload: Parameters<typeof applicationService.update>[1] }) =>
      applicationService.update(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: applicationKeys.all }),
  }));
}

export function useDeleteApplication() {
  const queryClient = useQueryClient();
  return useMutation(() => ({
    mutationFn: applicationService.delete,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: applicationKeys.all }),
  }));
}
