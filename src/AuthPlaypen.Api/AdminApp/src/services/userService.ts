import { http } from "@/services/http";
import type { AdminUser } from "@/types/models";

export const userService = {
  getCurrent: () => http<AdminUser>("/user"),
};
