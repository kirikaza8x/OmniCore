import { http } from "@/shared/api/http";
import type { CurrentAccount } from "./types";
import type { ForgotPasswordInput, LoginInput, RegisterInput, ResetPasswordInput, VerifyEmailInput } from "./schemas";

export const authApi = {
  login: (input: LoginInput) => http<void>("/api/auth/login", { method: "POST", json: input }),
  register: (input: RegisterInput) => http<void>("/api/auth/register", { method: "POST", json: input }),
  /** The signed-in account (through the proxy, which adds the bearer token). */
  me: () => http<CurrentAccount>("/api/proxy/auth/me"),
  forgotPassword: (input: ForgotPasswordInput) => http<void>("/api/auth/forgot-password", { method: "POST", json: input }),
  resetPassword: (input: ResetPasswordInput) => http<void>("/api/auth/reset-password", { method: "POST", json: input }),
  verifyEmail: (input: VerifyEmailInput) => http<void>("/api/auth/verify-email", { method: "POST", json: input }),
  /** Signed-in users only: goes through the proxy, which adds the bearer token. */
  requestEmailVerification: () => http<void>("/api/proxy/auth/email/verify/request", { method: "POST" }),
  logout: () => http<void>("/api/auth/logout", { method: "POST" }),
};
