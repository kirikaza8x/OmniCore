import { describe, expect, it } from "vitest";
import { themeToDarkClass } from "../theme";

describe("themeToDarkClass", () => {
  it("Dark is always dark and Light is never dark, whatever the OS says", () => {
    expect(themeToDarkClass("Dark", false)).toBe(true);
    expect(themeToDarkClass("Dark", true)).toBe(true);
    expect(themeToDarkClass("Light", true)).toBe(false);
    expect(themeToDarkClass("Light", false)).toBe(false);
  });

  it("System (and anything unknown or missing) follows the OS", () => {
    expect(themeToDarkClass("System", true)).toBe(true);
    expect(themeToDarkClass("System", false)).toBe(false);
    expect(themeToDarkClass("nonsense", true)).toBe(true);
    expect(themeToDarkClass(undefined, false)).toBe(false);
    expect(themeToDarkClass(null, true)).toBe(true);
  });
});
