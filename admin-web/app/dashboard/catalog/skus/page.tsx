"use client";

import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "../../../components/page_header";
import { Card } from "../../../components/card";
import { Modal } from "../../../components/modal";
import { errorMessage } from "../../../lib/api";
import { formatMoney } from "../../lib/dashboard";
import {
  createSku,
  listCategories,
  updateSku,
  type CategoryResult,
  type StockBaseUnit,
  searchSkus,
  type SkuResult,
  type SkuSearchPage,
} from "../../lib/catalog";
import { readSession, hasPermission } from "../../../lib/auth";

export default function AdminSkusPage() {
  const session = useMemo(() => readSession(), []);
  const canWrite = !!session && hasPermission(session, "catalog:write");

  const [query, setQuery] = useState("");
  const [includeInactive, setIncludeInactive] = useState(true);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState<SkuSearchPage | null>(null);

  const [categories, setCategories] = useState<CategoryResult[]>([]);

  const [createOpen, setCreateOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [createCategoryCode, setCreateCategoryCode] = useState("");
  const [createName, setCreateName] = useState("");
  const [createPrice, setCreatePrice] = useState("");
  const [createActive, setCreateActive] = useState(true);
  const [createHasOwnStock, setCreateHasOwnStock] = useState(true);
  const [createStockBaseUnit, setCreateStockBaseUnit] = useState<StockBaseUnit>("Unit");
  const [createInitialStock, setCreateInitialStock] = useState("0");
  const [createdSkuCode, setCreatedSkuCode] = useState<string | null>(null);

  const [stockOpen, setStockOpen] = useState(false);
  const [stockSku, setStockSku] = useState<SkuResult | null>(null);
  const [stockBaseUnit, setStockBaseUnit] = useState<StockBaseUnit>("Unit");
  const [stockOnHand, setStockOnHand] = useState("0");
  const [savingStock, setSavingStock] = useState(false);

  const canLoadMore = useMemo(() => {
    return !!page?.nextCursorCode && !!page?.nextCursorId;
  }, [page]);

  async function loadFirst() {
    setLoading(true);
    setError(null);
    try {
      const resp = await searchSkus({
        query,
        limit: 50,
        includeInactive,
      });
      setPage(resp);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setLoading(false);
    }
  }

  async function loadMore() {
    if (!canLoadMore || loadingMore) return;
    setLoadingMore(true);
    setError(null);
    try {
      const resp = await searchSkus({
        query,
        limit: 50,
        cursorCode: page?.nextCursorCode ?? null,
        cursorId: page?.nextCursorId ?? null,
        includeInactive,
      });
      setPage((prev) => ({
        items: [...(prev?.items ?? []), ...resp.items],
        nextCursorCode: resp.nextCursorCode ?? null,
        nextCursorId: resp.nextCursorId ?? null,
      }));
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setLoadingMore(false);
    }
  }

  useEffect(() => {
    loadFirst();
    listCategories({ includeInactive: true })
      .then((cs) => {
        setCategories(cs);
        if (cs.length > 0 && createCategoryCode.trim().length === 0) {
          setCreateCategoryCode(cs[0]!.code);
        }
      })
      .catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Produtos (SKUs)"
        subtitle="Gerencie itens de venda e estoque. Estoque próprio para itens unitários/insumos, ou receita para porções."
        actions={
          <>
            <button
              className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 bg-white px-4 text-sm font-medium text-zinc-900 hover:bg-zinc-50 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
              onClick={loadFirst}
              disabled={loading}
            >
              Atualizar
            </button>
            <button
              className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
              onClick={() => {
                if (!canWrite) {
                  setError("Sem permissão para criar SKU (catalog:write).");
                  return;
                }
                setCreateOpen(true);
                setCreatedSkuCode(null);
              }}
              disabled={!canWrite}
            >
              Novo SKU
            </button>
          </>
        }
      />

      <Card>
        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <input
            className="h-10 flex-1 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Buscar por código ou nome"
          />
          <label className="flex items-center gap-2 text-sm text-zinc-700 dark:text-zinc-200">
            <input
              type="checkbox"
              checked={includeInactive}
              onChange={(e) => setIncludeInactive(e.target.checked)}
            />
            Incluir inativos
          </label>
          <button
            className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 bg-white px-4 text-sm font-medium text-zinc-900 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
            onClick={loadFirst}
            disabled={loading}
          >
            Buscar
          </button>
        </div>
      </Card>

      {error ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-200">
          {error}
        </div>
      ) : null}

      <Card title="Lista">
        {loading && !page ? (
          <div className="text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div>
        ) : null}
        {!loading && page && page.items.length === 0 ? (
          <div className="text-sm text-zinc-600 dark:text-zinc-400">Sem resultados</div>
        ) : null}
        {page ? (
          <SkuTable
            items={page.items}
            onConfigureStock={(sku) => {
              if (!canWrite) {
                setError("Sem permissão para configurar estoque (catalog:write).");
                return;
              }
              setStockSku(sku);
              setStockBaseUnit(
                typeof sku.stockBaseUnit === "string" ? (sku.stockBaseUnit as StockBaseUnit) : "Unit",
              );
              setStockOnHand(String(sku.stockOnHandBaseQty ?? 0));
              setStockOpen(true);
            }}
          />
        ) : null}
        <div className="mt-4 flex justify-end">
          <button
            className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
            onClick={loadMore}
            disabled={!canLoadMore || loadingMore}
          >
            {loadingMore ? "Carregando…" : "Carregar mais"}
          </button>
        </div>
      </Card>

      <Modal
        open={createOpen}
        title="Criar SKU"
        onClose={() => setCreateOpen(false)}
        footer={
          <>
            <button
              className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 bg-white px-4 text-sm font-medium text-zinc-900 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
              onClick={() => setCreateOpen(false)}
              disabled={creating}
            >
              Cancelar
            </button>
            <button
              className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
              onClick={async () => {
                if (creating) return;
                const name = createName.trim();
                if (name.length < 2) {
                  setError("Nome inválido.");
                  return;
                }
                const price = Number(createPrice.replace(".", "").replace(",", "."));
                if (!Number.isFinite(price) || price < 0) {
                  setError("Preço inválido.");
                  return;
                }
                const priceCents = Math.round(price * 100);
                const categoryCode = createCategoryCode.trim();
                if (!categoryCode) {
                  setError("Categoria inválida.");
                  return;
                }

                setCreating(true);
                setError(null);
                try {
                  const stockBaseUnit = createHasOwnStock ? createStockBaseUnit : null;
                  const stockOnHandBaseQty = createHasOwnStock
                    ? Number(createInitialStock.replace(",", "."))
                    : null;
                  const resp = await createSku({
                    categoryCode,
                    name,
                    priceCents,
                    isActive: createActive,
                    tracksStock: createHasOwnStock,
                    stockBaseUnit,
                    stockOnHandBaseQty: createHasOwnStock
                      ? Number.isFinite(stockOnHandBaseQty as number)
                        ? (stockOnHandBaseQty as number)
                        : 0
                      : null,
                  });
                  setCreatedSkuCode(resp.code);
                  setCreateName("");
                  setCreatePrice("");
                  setCreateInitialStock("0");
                  await loadFirst();
                } catch (e) {
                  setError(errorMessage(e));
                } finally {
                  setCreating(false);
                }
              }}
              disabled={creating}
            >
              {creating ? "Salvando…" : "Criar"}
            </button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          {createdSkuCode ? (
            <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:border-emerald-900/60 dark:bg-emerald-950/40 dark:text-emerald-200">
              SKU criado: <span className="font-semibold">{createdSkuCode}</span>. Se for uma porção que consome outro SKU, configure a receita em <span className="font-semibold">Estoque</span>.
            </div>
          ) : null}

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Categoria</span>
            <select
              className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              value={createCategoryCode}
              onChange={(e) => setCreateCategoryCode(e.target.value)}
              disabled={creating}
            >
              {categories.map((c) => (
                <option key={c.id} value={c.code}>
                  {c.code} — {c.name}
                </option>
              ))}
            </select>
          </label>

          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <label className="flex flex-col gap-1">
              <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Nome</span>
              <input
                className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                value={createName}
                onChange={(e) => setCreateName(e.target.value)}
                disabled={creating}
              />
            </label>
            <label className="flex flex-col gap-1">
              <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Preço (R$)</span>
              <input
                className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                value={createPrice}
                onChange={(e) => setCreatePrice(e.target.value)}
                disabled={creating}
                placeholder="ex.: 12,90"
              />
            </label>
          </div>

          <label className="flex items-center gap-2 text-sm text-zinc-700 dark:text-zinc-200">
            <input
              type="checkbox"
              checked={createActive}
              onChange={(e) => setCreateActive(e.target.checked)}
              disabled={creating}
            />
            Ativo
          </label>

          <div className="rounded-xl border border-zinc-200 bg-zinc-50 p-4 dark:border-zinc-800 dark:bg-zinc-900">
            <div className="text-sm font-semibold text-zinc-950 dark:text-zinc-50">Estoque</div>
            <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
              Marque estoque próprio quando o saldo é contado no próprio SKU (ex.: refrigerante, insumo base). Desmarque quando o SKU é uma porção que consome outro SKU (receita).
            </div>
            <div className="mt-3 flex flex-col gap-3">
              <label className="flex items-center gap-2 text-sm text-zinc-700 dark:text-zinc-200">
                <input
                  type="checkbox"
                  checked={createHasOwnStock}
                  onChange={(e) => setCreateHasOwnStock(e.target.checked)}
                  disabled={creating}
                />
                Controlar estoque neste SKU (estoque próprio)
              </label>
              {createHasOwnStock ? (
                <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                  <label className="flex flex-col gap-1">
                    <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Unidade base</span>
                    <select
                      className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                      value={createStockBaseUnit}
                      onChange={(e) => setCreateStockBaseUnit(e.target.value as StockBaseUnit)}
                      disabled={creating}
                    >
                      <option value="Unit">Unidade</option>
                      <option value="Gram">Peso (g)</option>
                      <option value="Milliliter">Volume (ml)</option>
                    </select>
                  </label>
                  <label className="flex flex-col gap-1">
                    <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Estoque inicial (na unidade base)</span>
                    <input
                      className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                      value={createInitialStock}
                      onChange={(e) => setCreateInitialStock(e.target.value)}
                      disabled={creating}
                      placeholder="ex.: 100"
                    />
                  </label>
                </div>
              ) : (
                <div className="text-sm text-zinc-600 dark:text-zinc-400">
                  Depois de criar, configure a receita (consumo por venda) em <span className="font-semibold">Estoque</span>.
                </div>
              )}
            </div>
          </div>
        </div>
      </Modal>

      <Modal
        open={stockOpen}
        title="Configurar estoque próprio"
        onClose={() => setStockOpen(false)}
        footer={
          <>
            <button
              className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 bg-white px-4 text-sm font-medium text-zinc-900 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
              onClick={() => setStockOpen(false)}
              disabled={savingStock}
            >
              Cancelar
            </button>
            <button
              className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
              onClick={async () => {
                if (!stockSku) return;
                const qty = Number(stockOnHand.replace(",", "."));
                if (!Number.isFinite(qty) || qty < 0) {
                  setError("Quantidade inválida.");
                  return;
                }
                setSavingStock(true);
                setError(null);
                try {
                  const updated = await updateSku({
                    id: stockSku.id,
                    categoryCode: stockSku.categoryCode,
                    name: stockSku.name,
                    priceCents: stockSku.priceCents,
                    averagePrepSeconds: stockSku.averagePrepSeconds ?? null,
                    imageUrl: stockSku.imageUrl ?? null,
                    tracksStock: true,
                    stockBaseUnit,
                    stockOnHandBaseQty: qty,
                    isActive: stockSku.isActive,
                  });
                  setStockOpen(false);
                  setPage((prev) => {
                    if (!prev) return prev;
                    return {
                      ...prev,
                      items: prev.items.map((i) => (i.id === updated.id ? updated : i)),
                    };
                  });
                } catch (e) {
                  setError(errorMessage(e));
                } finally {
                  setSavingStock(false);
                }
              }}
              disabled={savingStock}
            >
              {savingStock ? "Salvando…" : "Salvar"}
            </button>
          </>
        }
      >
        {stockSku ? (
          <div className="flex flex-col gap-3">
            <div className="text-sm text-zinc-600 dark:text-zinc-400">
              SKU: <span className="font-semibold text-zinc-900 dark:text-zinc-50">{stockSku.code}</span> — {stockSku.name}
            </div>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <label className="flex flex-col gap-1">
                <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Unidade base</span>
                <select
                  className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                  value={stockBaseUnit}
                  onChange={(e) => setStockBaseUnit(e.target.value as StockBaseUnit)}
                  disabled={savingStock}
                >
                  <option value="Unit">Unidade</option>
                  <option value="Gram">Peso (g)</option>
                  <option value="Milliliter">Volume (ml)</option>
                </select>
              </label>
              <label className="flex flex-col gap-1">
                <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Saldo atual (na unidade base)</span>
                <input
                  className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                  value={stockOnHand}
                  onChange={(e) => setStockOnHand(e.target.value)}
                  disabled={savingStock}
                />
              </label>
            </div>
          </div>
        ) : null}
      </Modal>
    </div>
  );
}

function SkuTable(props: { items: SkuResult[]; onConfigureStock: (sku: SkuResult) => void }) {
  return (
    <div className="overflow-hidden rounded-xl border border-zinc-200 dark:border-zinc-800">
      <div className="grid grid-cols-12 gap-2 border-b border-zinc-200 bg-zinc-50 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
        <div className="col-span-2">Código</div>
        <div className="col-span-4">Nome</div>
        <div className="col-span-2">Categoria</div>
        <div className="col-span-2 text-right">Preço</div>
        <div className="col-span-1 text-right">Ativo</div>
        <div className="col-span-1 text-right">Estoque</div>
      </div>
      {props.items.map((s) => (
        <div
          key={s.id}
          className="grid grid-cols-12 gap-2 border-b border-zinc-100 px-3 py-2 text-sm text-zinc-900 last:border-b-0 dark:border-zinc-900 dark:text-zinc-50"
        >
          <div className="col-span-2 font-medium">{s.code}</div>
          <div className="col-span-4 truncate">{s.name}</div>
          <div className="col-span-2">{s.categoryCode}</div>
          <div className="col-span-2 text-right">{formatMoney(s.priceCents)}</div>
          <div className="col-span-1 text-right">{s.isActive ? "Sim" : "Não"}</div>
          <div className="col-span-1 text-right">
            <button
              className="rounded-lg border border-zinc-200 px-2 py-1 text-xs font-medium text-zinc-900 hover:bg-zinc-50 dark:border-zinc-800 dark:text-zinc-50 dark:hover:bg-zinc-900"
              onClick={() => props.onConfigureStock(s)}
            >
              Configurar
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
