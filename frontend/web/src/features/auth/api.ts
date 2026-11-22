import { http } from "@/shared/api/http";
import type { LoginInput, RegisterInput } from "./schemas";

export const authApi = {
  login: (input: LoginInput) => http<void>("/api/auth/login", { method: "POST", json: input }),
  register: (input: RegisterInput) => http<void>("/api/auth/register", { method: "POST", json: input }),
  logout: () => http<void>("/api/auth/logout", { method: "POST" }),
};
