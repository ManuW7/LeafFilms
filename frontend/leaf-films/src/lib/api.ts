function getToken(): string | null {
  return localStorage.getItem("token");
}

export async function apiFetch<T>(
  method: string,
  path: string,
  body?: unknown,
  token?: string,
): Promise<T> {
  token = token ?? getToken() ?? undefined;

  const res = await fetch(path, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
  });

  if (res.status === 401) {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    window.location.href = "/login";
    throw new Error("Unauthorized");
  }

  if (!res.ok) {
    let errorData: any = {};
    try {
      errorData = await res.json();
    } catch {}
    const err: any = new Error(errorData?.error || `HTTP ${res.status}`);
    err.data = errorData;
    throw err;
  }

  if (res.status === 204 || res.headers.get("content-length") === "0") {
    return undefined as T;
  }

  return res.json() as Promise<T>;
}
