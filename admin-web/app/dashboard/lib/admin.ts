import { apiFetch } from "../../lib/api";

export type MeResponse = {
  tenantId: string;
  userId: string;
  email: string;
  role?: string | null;
  permissions: string[];
};

export async function getMe() {
  return apiFetch<MeResponse>("/api/users/me");
}

export async function createUser(params: { email: string; password: string; role: string }) {
  return apiFetch<{ userId: string; email: string; role: string }>("/api/users", {
    method: "POST",
    body: JSON.stringify(params),
  });
}

export type KitchenSla = {
  queuedTargetSeconds: number;
  preparationBaseTargetSeconds: number;
  readyTargetSeconds: number;
};

export async function getKitchenSla() {
  return apiFetch<KitchenSla>("/api/admin/kitchen-sla");
}

export async function upsertKitchenSla(params: KitchenSla) {
  return apiFetch<KitchenSla>("/api/admin/kitchen-sla", {
    method: "PUT",
    body: JSON.stringify(params),
  });
}

