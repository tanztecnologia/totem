import { apiFetch } from "../../lib/api";

export type PaymentMethod = "CreditCard" | "DebitCard" | "Pix" | "Cash";

export type KitchenStatus =
  | "PendingPayment"
  | "Queued"
  | "InPreparation"
  | "Ready"
  | "Completed"
  | "Cancelled";

export type OrderStatus = "Created" | "Paid" | "Cancelled";

export type DashboardOverview = {
  fromInclusive: string;
  toInclusive: string;
  ordersCount: number;
  paidOrdersCount: number;
  cancelledOrdersCount: number;
  revenueCents: number;
  averageTicketCents: number;
  paymentsByMethod: Array<{
    method: PaymentMethod;
    amountCents: number;
    paymentsCount: number;
  }>;
  paymentsByProvider: Array<{
    provider: string;
    amountCents: number;
    paymentsCount: number;
  }>;
  ordersByKitchenStatus: Array<{
    kitchenStatus: KitchenStatus;
    ordersCount: number;
  }>;
};

export type DashboardOrdersPage = {
  items: Array<{
    orderId: string;
    comanda?: string | null;
    status: OrderStatus;
    kitchenStatus: KitchenStatus;
    totalCents: number;
    createdAt: string;
    updatedAt: string;
    paymentStatus?: string | null;
    paymentMethod?: PaymentMethod | null;
    paymentAmountCents?: number | null;
    paymentProvider?: string | null;
  }>;
  nextCursorUpdatedAt?: string | null;
  nextCursorOrderId?: string | null;
};

export async function getDashboardOverview(params: { from: string; to: string }) {
  const q = new URLSearchParams({ from: params.from, to: params.to });
  return apiFetch<DashboardOverview>(`/api/dashboard/overview?${q.toString()}`);
}

export async function getDashboardOrders(params: {
  limit: number;
  cursorUpdatedAt?: string | null;
  cursorOrderId?: string | null;
}) {
  const q = new URLSearchParams({ limit: String(params.limit) });
  if (params.cursorUpdatedAt) q.set("cursorUpdatedAt", params.cursorUpdatedAt);
  if (params.cursorOrderId) q.set("cursorOrderId", params.cursorOrderId);
  return apiFetch<DashboardOrdersPage>(`/api/dashboard/orders?${q.toString()}`);
}

export function formatMoney(cents: number): string {
  const value = cents / 100;
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value);
}

export function formatDateTime(iso: string): string {
  const dt = new Date(iso);
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(dt);
}

