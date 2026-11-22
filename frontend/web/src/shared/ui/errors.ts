import { ApiError } from "@/shared/api/problem";

/** One readable message for any failure. */
export function messageOf(error: unknown): string {
  if (error instanceof ApiError) {
    const fields = error.fieldMessages;
    if (fields.length > 0) return fields.join(" ");
    return error.problem.detail || error.problem.title;
  }
  return "Something went wrong. Please try again.";
}
