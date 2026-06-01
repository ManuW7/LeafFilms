function getToken(): string | null {
  return localStorage.getItem("token");
}

function toApiPath(path: string): string {
  if (/^https?:\/\//i.test(path)) return path;
  return path.startsWith("/api") ? path : `/api${path}`;
}

export class ApiError extends Error {
  status?: number;
  data?: { error?: string; [key: string]: unknown };
}

export async function apiFetch<T>(
  method: string,
  path: string,
  body?: unknown,
  token?: string,
): Promise<T> {
  token = token ?? getToken() ?? undefined;

  const res = await fetch(toApiPath(path), {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
  });

  if (res.status === 401) {
    const err = new ApiError("Unauthorized");
    err.status = 401;
    if (path !== "/users/login") {
      localStorage.removeItem("token");
      localStorage.removeItem("user");
      window.dispatchEvent(new Event("auth:unauthorized"));
    }
    throw err;
  }

  if (!res.ok) {
    let errorData: { error?: string; [key: string]: unknown } = {};
    try {
      errorData = await res.json();
    } catch {
      // Some endpoints return an empty error body.
    }
    const err = new ApiError(errorData?.error || `HTTP ${res.status}`);
    err.data = errorData;
    throw err;
  }

  if (res.status === 204 || res.headers.get("content-length") === "0") {
    return undefined as T;
  }

  return res.json() as Promise<T>;
}
