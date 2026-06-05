const API_BASE_URL = "/api";
const TOKEN_STORAGE_KEY = "aureus.token";

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
  anonymous?: boolean;
};

export async function apiFetch<TResponse>(path: string, options: RequestOptions = {}): Promise<TResponse> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (!options.anonymous) {
    const token = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (token !== null) {
      headers["Authorization"] = `Bearer ${token}`;
    }
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (!response.ok) {
    let problem: ProblemDetails | undefined;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = undefined;
    }

    if (response.status === 401) {
      window.dispatchEvent(new CustomEvent("aureus:unauthorized"));
    }

    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}
