/** The backend answers failures as RFC 7807 problem details: { title, detail, status, errors? }. */
export type Problem = {
  status: number;
  title: string;
  detail?: string;
  errors?: unknown;
};

export class ApiError extends Error {
  constructor(public readonly problem: Problem) {
    super(problem.detail || problem.title);
    this.name = "ApiError";
  }
  get status() {
    return this.problem.status;
  }
  /** Field messages from validation errors, whatever shape the backend used. */
  get fieldMessages(): string[] {
    const out: string[] = [];
    const walk = (v: unknown) => {
      if (typeof v === "string") out.push(v);
      else if (Array.isArray(v)) v.forEach(walk);
      else if (v && typeof v === "object") {
        const o = v as Record<string, unknown>;
        if (typeof o.description === "string") out.push(o.description);
        else if (typeof o.errorMessage === "string") out.push(o.errorMessage);
        else Object.values(o).forEach(walk);
      }
    };
    walk(this.problem.errors);
    return [...new Set(out)];
  }
}

export async function toProblem(response: Response): Promise<Problem> {
  try {
    const body = (await response.clone().json()) as Partial<Problem>;
    return {
      status: response.status,
      title: body.title ?? response.statusText ?? "Request failed",
      detail: body.detail,
      errors: body.errors,
    };
  } catch {
    return { status: response.status, title: response.statusText || "Request failed" };
  }
}
