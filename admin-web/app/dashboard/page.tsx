"use client";

import { useEffect, useMemo, useState } from "react";
import { errorMessage } from "../lib/api";
import { getDashboardOverview, formatMoney, type DashboardOverview } from "./lib/dashboard";

function startOfDayIso(d: Date) {
  const dd = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0);
  return dd.toISOString();
}

function endOfDayIso(d: Date) {
  const dd = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 23, 59, 59);
  return dd.toISOString();
}

function formatDateInput(d: Date) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export default function DashboardOverviewPage() {
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return formatDateInput(d);
  });
  const [toDate, setToDate] = useState(() => formatDateInput(new Date()));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [overview, setOverview] = useState<DashboardOverview | null>(null);

  const query = useMemo(() => {
    const from = startOfDayIso(new Date(fromDate));
    const to = endOfDayIso(new Date(toDate));
    return { from, to };
  }, [fromDate, toDate]);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      setLoading(true);
      setError(null);
      try {
        const resp = await getDashboardOverview(query);
        if (cancelled) return;
        setOverview(resp);
      } catch (e2) {
        if (cancelled) return;
        setError(errorMessage(e2));
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, [query]);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 className="text-xl font-semibold tracking-tight text-zinc-950 dark:text-zinc-50">
            Visão Geral
          </h1>
          <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            Indicadores e faturamento do período.
          </p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row">
          <label className="flex flex-col gap-1">
            <span className="text-xs font-medium text-zinc-700 dark:text-zinc-300">De</span>
            <input
              className="h-10 w-44 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1">
            <span className="text-xs font-medium text-zinc-700 dark:text-zinc-300">Até</span>
            <input
              className="h-10 w-44 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
            />
          </label>
        </div>
      </div>

      {error ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-200">
          {error}
        </div>
      ) : null}

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <KpiCard label="Faturamento" value={overview ? formatMoney(overview.revenueCents) : "—"} />
        <KpiCard label="Ticket médio" value={overview ? formatMoney(overview.averageTicketCents) : "—"} />
        <KpiCard label="Pedidos" value={overview ? String(overview.ordersCount) : "—"} />
        <KpiCard label="Pagos" value={overview ? String(overview.paidOrdersCount) : "—"} />
      </div>

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        <Card title="Pagamentos por método">
          {loading && !overview ? <div className="text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div> : null}
          {!loading && overview && overview.paymentsByMethod.length === 0 ? (
            <div className="text-sm text-zinc-600 dark:text-zinc-400">Sem dados</div>
          ) : null}
          {overview ? (
            <div className="flex flex-col gap-2">
              {overview.paymentsByMethod.map((p) => (
                <div key={p.method} className="flex items-center justify-between text-sm">
                  <span className="text-zinc-700 dark:text-zinc-200">{methodLabel(p.method)}</span>
                  <span className="text-zinc-900 dark:text-zinc-50">
                    {formatMoney(p.amountCents)} ({p.paymentsCount})
                  </span>
                </div>
              ))}
            </div>
          ) : null}
        </Card>

        <Card title="Pedidos por status de cozinha">
          {loading && !overview ? <div className="text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div> : null}
          {!loading && overview && overview.ordersByKitchenStatus.length === 0 ? (
            <div className="text-sm text-zinc-600 dark:text-zinc-400">Sem dados</div>
          ) : null}
          {overview ? (
            <div className="flex flex-wrap gap-2">
              {overview.ordersByKitchenStatus.map((k) => (
                <span
                  key={k.kitchenStatus}
                  className="rounded-full border border-zinc-200 bg-zinc-50 px-3 py-1 text-xs font-medium text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-200"
                >
                  {kitchenLabel(k.kitchenStatus)}: {k.ordersCount}
                </span>
              ))}
            </div>
          ) : null}
        </Card>
      </div>

      <Card title="Pagamentos por provedor">
        {loading && !overview ? <div className="text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div> : null}
        {!loading && overview && overview.paymentsByProvider.length === 0 ? (
          <div className="text-sm text-zinc-600 dark:text-zinc-400">Sem dados</div>
        ) : null}
        {overview ? (
          <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
            {overview.paymentsByProvider.map((p) => (
              <div key={p.provider} className="flex items-center justify-between rounded-lg border border-zinc-200 px-3 py-2 dark:border-zinc-800">
                <span className="text-sm text-zinc-700 dark:text-zinc-200">{p.provider || "Outro"}</span>
                <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">
                  {formatMoney(p.amountCents)} ({p.paymentsCount})
                </span>
              </div>
            ))}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function Card(props: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-2xl border border-zinc-200 bg-white p-4 shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mb-3 text-sm font-semibold text-zinc-950 dark:text-zinc-50">{props.title}</div>
      {props.children}
    </section>
  );
}

function KpiCard(props: { label: string; value: string }) {
  return (
    <section className="rounded-2xl border border-zinc-200 bg-white p-4 shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
      <div className="text-xs font-medium text-zinc-600 dark:text-zinc-400">{props.label}</div>
      <div className="mt-1 text-lg font-semibold text-zinc-950 dark:text-zinc-50">{props.value}</div>
    </section>
  );
}

function methodLabel(m: string) {
  switch (m) {
    case "CreditCard":
      return "Crédito";
    case "DebitCard":
      return "Débito";
    case "Pix":
      return "Pix";
    case "Cash":
      return "Dinheiro";
    default:
      return m;
  }
}

function kitchenLabel(s: string) {
  switch (s) {
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
      return s;
  }
}
