import { http } from "@/shared/api/http";
import type { CurrentAccount, TenantMembership } from "./types";
import type { ForgotPasswordInput, LoginInput, RegisterInput, ResendVerificationInput, ResetPasswordInput, SwitchTenantInput, VerifyEmailInput } from "./schemas";

export const authApi = {
  login: (input: LoginInput) => http<void>("/api/auth/login", { method: "POST", json: input }),
  register: (input: RegisterInput) => http<void>("/api/auth/register", { method: "POST", json: input }),
  /** The signed-in account (through the proxy, which adds the bearer token). */
  me: () => http<CurrentAccount>("/api/proxy/auth/me"),
  forgotPassword: (input: ForgotPasswordInput) => http<void>("/api/auth/forgot-password", { method: "POST", json: input }),
  resetPassword: (input: ResetPasswordInput) => http<void>("/api/auth/reset-password", { method: "POST", json: input }),
  resendVerification: (input: ResendVerificationInput) => http<void>("/api/auth/resend-verification", { method: "POST", json: input }),
  verifyEmail: (input: VerifyEmailInput) => http<void>("/api/auth/verify-email", { method: "POST", json: input }),
  /** Signed-in users only: goes through the proxy, which adds the bearer token. */
  requestEmailVerification: () => http<void>("/api/proxy/auth/email/verify/request", { method: "POST" }),
  /** Tenants the signed-in account belongs to. */
  tenants: () => http<TenantMembership[]>("/api/proxy/auth/tenants"),
  /** Issues a new access token for another tenant (the refresh token is untouched). */
  switchTenant: (input: SwitchTenantInput) => http<void>("/api/auth/switch-tenant", { method: "POST", json: input }),
  logout: () => http<void>("/api/auth/logout", { method: "POST" }),
};
