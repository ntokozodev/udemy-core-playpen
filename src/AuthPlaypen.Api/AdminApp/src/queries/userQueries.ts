import { useQuery } from "@tanstack/solid-query";
import { userService } from "@/services/userService";

export const userKeys = {
  current: ["user", "current"] as const,
};

export function useCurrentUser() {
  return useQuery(() => ({
    queryKey: userKeys.current,
    queryFn: userService.getCurrent,
  }));
}
