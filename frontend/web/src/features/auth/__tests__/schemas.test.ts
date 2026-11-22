import { describe, expect, it } from "vitest";
import { loginSchema, registerSchema } from "../schemas";

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
