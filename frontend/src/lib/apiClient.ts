const API_BASE_URL = "/api";

export type ProblemDetails = {
  status?: number;
  title?: string;
  detail?: string;
  instance?: string;
};

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly detail?: string;

  constructor(status: number, problem?: ProblemDetails) {
    super(problem?.detail ?? problem?.title ?? `Request failed with status ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.code = problem?.title;
    this.detail = problem?.detail;
  }
}

type RequestOptions = {
  method?: string;
  body?: unknown;
};

export async function apiFetch<TResponse>(path: string, options: RequestOptions = {}): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "GET",
    headers: { "Content-Type": "application/json" },
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (!response.ok) {
    let problem: ProblemDetails | undefined;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = undefined;
    }

    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}
