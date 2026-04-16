import { readSession } from "./auth";

export type ApiError = {
  status: number;
  message: string;
};

export type LoginResponse = {
  tenantId: string;
  userId: string;
  email: string;
  role: string;
  permissions: string[];
  token: string;
};

export function errorMessage(e: unknown): string {
  if (typeof e === "object" && e && "message" in e) {
    const msg = (e as { message: unknown }).message;
    return typeof msg === "string" ? msg : String(msg);
  }
  return String(e);
}

export function apiBaseUrl(): string {
  const v = process.env.NEXT_PUBLIC_API_BASE_URL;
  if (!v || v.trim().length === 0) return "http://localhost:5192";
  return v.trim().replace(/\/+$/, "");
}

async function readErrorMessage(resp: Response): Promise<string> {
  try {
    const json = (await resp.json()) as { error?: string };
    if (json?.error) return json.error;
  } catch {}
  return `${resp.status} ${resp.statusText}`.trim();
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const session = readSession();
  const headers = new Headers(init?.headers);
  headers.set("Content-Type", "application/json");
  if (session?.token) headers.set("Authorization", `Bearer ${session.token}`);

  const resp = await fetch(`${apiBaseUrl()}${path}`, { ...init, headers });
  if (!resp.ok) {
    const message = await readErrorMessage(resp);
    throw { status: resp.status, message } satisfies ApiError;
  }
  return (await resp.json()) as T;
}

export async function apiFetchVoid(path: string, init?: RequestInit): Promise<void> {
  const session = readSession();
  const headers = new Headers(init?.headers);
  headers.set("Content-Type", "application/json");
  if (session?.token) headers.set("Authorization", `Bearer ${session.token}`);

  const resp = await fetch(`${apiBaseUrl()}${path}`, { ...init, headers });
  if (!resp.ok) {
    const message = await readErrorMessage(resp);
    throw { status: resp.status, message } satisfies ApiError;
  }
}

export async function apiFetchFormData<T>(path: string, formData: FormData, init?: RequestInit): Promise<T> {
  const session = readSession();
  const headers = new Headers(init?.headers);
  if (session?.token) headers.set("Authorization", `Bearer ${session.token}`);

  const resp = await fetch(`${apiBaseUrl()}${path}`, { ...init, method: init?.method ?? "POST", body: formData, headers });
  if (!resp.ok) {
    const message = await readErrorMessage(resp);
    throw { status: resp.status, message } satisfies ApiError;
  }
  return (await resp.json()) as T;
}

export async function login(params: {
  tenantName: string;
  email: string;
  password: string;
}): Promise<LoginResponse> {
  return apiFetch<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify(params),
  });
}
