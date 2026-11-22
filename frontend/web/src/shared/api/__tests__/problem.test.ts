import { describe, expect, it } from "vitest";
import { ApiError, toProblem } from "../problem";

describe("toProblem", () => {
  it("reads problem details", async () => {
    const response = new Response(JSON.stringify({ title: "Conflict", detail: "Taken", status: 409 }), { status: 409 });
    expect(await toProblem(response)).toMatchObject({ status: 409, title: "Conflict", detail: "Taken" });
  });

  it("survives a body that is not JSON", async () => {
    const problem = await toProblem(new Response("<html>bad gateway</html>", { status: 502, statusText: "Bad Gateway" }));
    expect(problem).toMatchObject({ status: 502, title: "Bad Gateway" });
  });
});

describe("ApiError.fieldMessages", () => {
  it("collects messages from different error shapes without duplicates", () => {
    const error = new ApiError({
      status: 400,
      title: "Validation",
      errors: [{ code: "A", description: "Name is required." }, "Name is required.", { x: ["Too long."] }],
    });
    expect(error.fieldMessages).toEqual(["Name is required.", "Too long."]);
  });

  it("uses the detail as the message", () => {
    expect(new ApiError({ status: 401, title: "Unauthorized", detail: "Invalid username, email or password." }).message)
      .toBe("Invalid username, email or password.");
  });
});
