"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { errorMessage } from "../../lib/api";
import { formatDateTime, formatMoney, getDashboardOrders, type DashboardOrdersPage } from "../lib/dashboard";

export default function DashboardOrdersPage() {
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState<DashboardOrdersPage | null>(null);

  const canLoadMore = useMemo(() => {
    return !!page?.nextCursorUpdatedAt && !!page?.nextCursorOrderId;
  }, [page]);

  const refresh = useCallback(async (isBackground?: boolean) => {
    if (isBackground) setRefreshing(true);
    else setLoading(true);
    setError(null);
    try {
      const resp = await getDashboardOrders({ limit: 50 });
      setPage(resp);
    } catch (e2) {
      setError(errorMessage(e2));
    } finally {
      if (isBackground) setRefreshing(false);
      else setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh(false);
    const id = setInterval(() => {
      refresh(true);
    }, 10_000);
    return () => clearInterval(id);
  }, [refresh]);

  async function loadMore() {
    if (!canLoadMore || loadingMore) return;
    setLoadingMore(true);
    setError(null);
    try {
      const resp = await getDashboardOrders({
        limit: 50,
        cursorUpdatedAt: page?.nextCursorUpdatedAt ?? null,
        cursorOrderId: page?.nextCursorOrderId ?? null,
      });
      setPage((prev) => {
        const prevItems = prev?.items ?? [];
        return {
          items: [...prevItems, ...resp.items],
          nextCursorUpdatedAt: resp.nextCursorUpdatedAt ?? null,
          nextCursorOrderId: resp.nextCursorOrderId ?? null,
        };
      });
    } catch (e2) {
      setError(errorMessage(e2));
    } finally {
      setLoadingMore(false);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-xl font-semibold tracking-tight text-zinc-950 dark:text-zinc-50">
          Pedidos
        </h1>
        <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
          Lista paginada por última atualização.
        </p>
        <div className="mt-3 flex items-center gap-2">
          <button
            className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 bg-white px-4 text-sm font-medium text-zinc-900 hover:bg-zinc-50 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
            onClick={() => refresh(false)}
            disabled={loading || refreshing}
          >
            {refreshing ? "Atualizando…" : "Atualizar"}
          </button>
          <div className="text-xs text-zinc-600 dark:text-zinc-400">Auto-atualiza a cada 10s</div>
        </div>
      </div>

      {error ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-200">
          {error}
        </div>
      ) : null}

      <section className="overflow-hidden rounded-2xl border border-zinc-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
        <div className="grid grid-cols-12 gap-2 border-b border-zinc-200 bg-zinc-50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
          <div className="col-span-4">Pedido</div>
          <div className="col-span-2">Status</div>
          <div className="col-span-2">Cozinha</div>
          <div className="col-span-2">Atualizado</div>
          <div className="col-span-2 text-right">Total</div>
        </div>

        {loading && !page ? (
          <div className="px-4 py-6 text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div>
        ) : null}

        {!loading && page && page.items.length === 0 ? (
          <div className="px-4 py-6 text-sm text-zinc-600 dark:text-zinc-400">Sem pedidos</div>
        ) : null}

        {page?.items.map((o) => (
          <div
            key={o.orderId}
            className="grid grid-cols-12 gap-2 border-b border-zinc-100 px-4 py-3 text-sm text-zinc-900 last:border-b-0 dark:border-zinc-900 dark:text-zinc-50"
          >
            <div className="col-span-4">
              <div className="font-medium">{o.orderId.slice(0, 8)}</div>
              <div className="text-xs text-zinc-600 dark:text-zinc-400">
                {o.comanda?.trim() ? `Comanda ${o.comanda}` : "—"}
              </div>
            </div>
            <div className="col-span-2">{statusLabel(o.status)}</div>
            <div className="col-span-2">{kitchenLabel(o.kitchenStatus)}</div>
            <div className="col-span-2 text-xs text-zinc-600 dark:text-zinc-400">{formatDateTime(o.updatedAt)}</div>
            <div className="col-span-2 text-right font-medium">{formatMoney(o.totalCents)}</div>
          </div>
        ))}
      </section>

      <div className="flex items-center justify-between">
        <div className="text-xs text-zinc-600 dark:text-zinc-400">
          {page ? `${page.items.length} pedidos` : ""}
        </div>
        <button
          className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
          onClick={loadMore}
          disabled={!canLoadMore || loadingMore}
        >
          {loadingMore ? "Carregando…" : "Carregar mais"}
        </button>
      </div>
    </div>
  );
}

function statusLabel(v: string) {
  switch (v) {
    case "Created":
      return "Criado";
    case "Paid":
      return "Pago";
    case "Cancelled":
      return "Cancelado";
    default:
      return v;
  }
}

function kitchenLabel(v: string) {
  switch (v) {
    case "PendingPayment":
      return "Aguardando";
    case "Queued":
      return "Fila";
    case "InPreparation":
      return "Em preparo";
    case "Ready":
      return "Pronto";
    case "Completed":
      return "Finalizado";
    case "Cancelled":
      return "Cancelado";
    default:
      return v;
  }
}
