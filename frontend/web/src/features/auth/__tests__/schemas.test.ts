import { describe, expect, it } from "vitest";
import {
  forgotPasswordSchema,
  loginSchema,
  registerSchema,
  resendVerificationSchema,
  resetPasswordFormSchema,
  resetPasswordSchema,
  switchTenantSchema,
  verifyEmailSchema,
} from "../schemas";

describe("registerSchema", () => {
  const valid = { username: "alice_01", email: "alice@example.com", password: "Sup3r!Secret" };

  it("accepts a valid registration", () => {
    expect(registerSchema.safeParse(valid).success).toBe(true);
  });

  it.each([
    ["short username", { ...valid, username: "ab" }],
    ["long username", { ...valid, username: "a".repeat(31) }],
    ["bad email", { ...valid, email: "not-an-email" }],
    ["no uppercase", { ...valid, password: "sup3r!secret" }],
    ["no digit", { ...valid, password: "Super!Secret" }],
    ["no special character", { ...valid, password: "Sup3rSecret1" }],
    ["too short", { ...valid, password: "Su3!" }],
  ])("rejects %s", (_name, input) => {
    expect(registerSchema.safeParse(input).success).toBe(false);
  });
});

describe("loginSchema", () => {
  it("needs both fields", () => {
    expect(loginSchema.safeParse({ identifier: "", password: "x" }).success).toBe(false);
    expect(loginSchema.safeParse({ identifier: "alice", password: "" }).success).toBe(false);
    expect(loginSchema.safeParse({ identifier: "alice", password: "x" }).success).toBe(true);
  });
});

describe("forgotPasswordSchema", () => {
  it("needs a valid email", () => {
    expect(forgotPasswordSchema.safeParse({ email: "alice@example.com" }).success).toBe(true);
    expect(forgotPasswordSchema.safeParse({ email: "nope" }).success).toBe(false);
  });
});

describe("resetPasswordSchema", () => {
  const valid = { token: "abc_DEF-123", newPassword: "Sup3r!Secret" };

  it("accepts a token and a strong password", () => {
    expect(resetPasswordSchema.safeParse(valid).success).toBe(true);
  });

  it.each([
    ["no token", { ...valid, token: "" }],
    ["huge token", { ...valid, token: "a".repeat(129) }],
    ["weak password", { ...valid, newPassword: "password" }],
  ])("rejects %s", (_name, input) => {
    expect(resetPasswordSchema.safeParse(input).success).toBe(false);
  });

  it("the form also needs the two passwords to match", () => {
    expect(resetPasswordFormSchema.safeParse({ ...valid, confirmPassword: valid.newPassword }).success).toBe(true);
    const mismatch = resetPasswordFormSchema.safeParse({ ...valid, confirmPassword: "Other!Passw0rd" });
    expect(mismatch.success).toBe(false);
  });
});

describe("verifyEmailSchema", () => {
  it("needs a token", () => {
    expect(verifyEmailSchema.safeParse({ token: "abc" }).success).toBe(true);
    expect(verifyEmailSchema.safeParse({ token: "" }).success).toBe(false);
  });
});

describe("resendVerificationSchema", () => {
  it("needs a valid email", () => {
    expect(resendVerificationSchema.safeParse({ email: "alice@example.com" }).success).toBe(true);
    expect(resendVerificationSchema.safeParse({ email: "" }).success).toBe(false);
  });
});

describe("switchTenantSchema", () => {
  it("accepts a uuid", () => {
    expect(switchTenantSchema.safeParse({ tenantId: "7e4a0001-0000-4000-8000-000000000001" }).success).toBe(true);
  });

  it.each([[{}], [{ tenantId: "" }], [{ tenantId: "default" }]])("rejects %j", (input) => {
    expect(switchTenantSchema.safeParse(input).success).toBe(false);
  });
});
