import { apiFetch } from "../../lib/api";

export type StockBaseUnit = "Unit" | "Gram" | "Milliliter";

export type SkuResult = {
  id: string;
  tenantId: string;
  categoryCode: string;
  code: string;
  name: string;
  priceCents: number;
  averagePrepSeconds?: number | null;
  imageUrl?: string | null;
  tracksStock: boolean;
  stockBaseUnit?: StockBaseUnit | number | null;
  stockOnHandBaseQty?: number | null;
  isActive: boolean;
};

export type SkuSearchPage = {
  items: SkuResult[];
  nextCursorCode?: string | null;
  nextCursorId?: string | null;
};

export type CategoryResult = {
  id: string;
  tenantId: string;
  code: string;
  slug: string;
  name: string;
  isActive: boolean;
};

export type SkuStockConsumption = {
  id: string;
  skuId: string;
  sourceSkuId: string;
  sourceSkuCode: string;
  quantityBase: number;
};

export async function getSkuByCode(code: string) {
  const q = new URLSearchParams({ code });
  return apiFetch<SkuResult>(`/api/skus/by-code?${q.toString()}`);
}

export async function addSkuStockEntry(params: { skuId: string; quantity: number; unit: string }) {
  return apiFetch<SkuResult>(`/api/skus/${params.skuId}/stock/entry`, {
    method: "POST",
    body: JSON.stringify({ quantity: params.quantity, unit: params.unit }),
  });
}

export async function listSkuStockConsumptions(skuId: string) {
  return apiFetch<SkuStockConsumption[]>(`/api/skus/${skuId}/stock/consumptions`);
}

export async function replaceSkuStockConsumptions(params: {
  skuId: string;
  items: Array<{ sourceSkuCode: string; quantity: number; unit: string }>;
}) {
  return apiFetch<SkuStockConsumption[]>(`/api/skus/${params.skuId}/stock/consumptions`, {
    method: "PUT",
    body: JSON.stringify({ items: params.items }),
  });
}

export async function searchSkus(params: {
  query?: string;
  limit: number;
  cursorCode?: string | null;
  cursorId?: string | null;
  includeInactive?: boolean;
}) {
  const q = new URLSearchParams({ limit: String(params.limit) });
  if (params.query && params.query.trim().length > 0) q.set("query", params.query.trim());
  if (params.cursorCode) q.set("cursorCode", params.cursorCode);
  if (params.cursorId) q.set("cursorId", params.cursorId);
  if (params.includeInactive !== undefined) q.set("includeInactive", String(params.includeInactive));
  return apiFetch<SkuSearchPage>(`/api/skus/search?${q.toString()}`);
}

export async function listCategories(params: { includeInactive: boolean }) {
  const q = new URLSearchParams({ includeInactive: String(params.includeInactive) });
  return apiFetch<CategoryResult[]>(`/api/categories?${q.toString()}`);
}

export async function createCategory(params: { name: string; slug?: string | null; isActive: boolean }) {
  return apiFetch<CategoryResult>("/api/categories", {
    method: "POST",
    body: JSON.stringify(params),
  });
}

export async function createSku(params: {
  categoryCode: string;
  name: string;
  priceCents: number;
  averagePrepSeconds?: number | null;
  imageUrl?: string | null;
  tracksStock?: boolean | null;
  stockBaseUnit?: StockBaseUnit | null;
  stockOnHandBaseQty?: number | null;
  isActive: boolean;
}) {
  return apiFetch<SkuResult>("/api/skus", {
    method: "POST",
    body: JSON.stringify(params),
  });
}

export async function updateSku(params: {
  id: string;
  categoryCode: string;
  name: string;
  priceCents: number;
  averagePrepSeconds?: number | null;
  imageUrl?: string | null;
  tracksStock?: boolean | null;
  stockBaseUnit?: StockBaseUnit | null;
  stockOnHandBaseQty?: number | null;
  isActive: boolean;
}) {
  return apiFetch<SkuResult>(`/api/skus/${params.id}`, {
    method: "PUT",
    body: JSON.stringify({
      categoryCode: params.categoryCode,
      name: params.name,
      priceCents: params.priceCents,
      averagePrepSeconds: params.averagePrepSeconds ?? null,
      imageUrl: params.imageUrl ?? null,
      tracksStock: params.tracksStock ?? null,
      stockBaseUnit: params.stockBaseUnit ?? null,
      stockOnHandBaseQty: params.stockOnHandBaseQty ?? null,
      isActive: params.isActive,
    }),
  });
}
