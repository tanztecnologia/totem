export type AuthSession = {
  tenantId: string;
  userId: string;
  email: string;
  role: string;
  permissions: string[];
  token: string;
};

const storageKey = "totem_admin_session";

export function readSession(): AuthSession | null {
  if (typeof window === "undefined") return null;
  const raw = window.localStorage.getItem(storageKey);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Partial<AuthSession>;
    if (!parsed.token || !parsed.tenantId || !parsed.userId) return null;
    return {
      tenantId: String(parsed.tenantId),
      userId: String(parsed.userId),
      email: String(parsed.email ?? ""),
      role: String(parsed.role ?? ""),
      permissions: Array.isArray(parsed.permissions)
        ? parsed.permissions.map((p) => String(p)).filter((p) => p.trim().length > 0)
        : [],
      token: String(parsed.token),
    };
  } catch {
    return null;
  }
}

export function writeSession(session: AuthSession) {
  window.localStorage.setItem(storageKey, JSON.stringify(session));
}

export function clearSession() {
  window.localStorage.removeItem(storageKey);
}

export function hasPermission(session: AuthSession | null, perm: string): boolean {
  if (!session) return false;
  return session.permissions.includes(perm);
}

