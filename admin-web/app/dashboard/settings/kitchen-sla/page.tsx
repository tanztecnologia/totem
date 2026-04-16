"use client";

import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "../../../components/page_header";
import { Card } from "../../../components/card";
import { errorMessage } from "../../../lib/api";
import { getKitchenSla, upsertKitchenSla } from "../../lib/admin";
import { readSession, hasPermission } from "../../../lib/auth";

export default function KitchenSlaPage() {
  const session = useMemo(() => readSession(), []);
  const canManage = !!session && hasPermission(session, "kitchen_sla:manage");

  const [queued, setQueued] = useState("120");
  const [prep, setPrep] = useState("480");
  const [ready, setReady] = useState("120");

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      setLoading(true);
      setError(null);
      try {
        const resp = await getKitchenSla();
        if (cancelled) return;
        setQueued(String(resp.queuedTargetSeconds));
        setPrep(String(resp.preparationBaseTargetSeconds));
        setReady(String(resp.readyTargetSeconds));
      } catch (e) {
        if (cancelled) return;
        setError(errorMessage(e));
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, []);

  async function onSave() {
    if (!canManage) {
      setError("Sem permissão para alterar SLA da cozinha.");
      return;
    }
    const q = Number(queued);
    const p = Number(prep);
    const r = Number(ready);
    if (!Number.isFinite(q) || q <= 0 || !Number.isFinite(p) || p <= 0 || !Number.isFinite(r) || r <= 0) {
      setError("Valores inválidos.");
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);
    try {
      await upsertKitchenSla({
        queuedTargetSeconds: q,
        preparationBaseTargetSeconds: p,
        readyTargetSeconds: r,
      });
      setSuccess("SLA atualizado.");
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Cozinha (SLA)"
        subtitle="Defina metas em segundos para fila, preparo e pronto."
      />

      {error ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-200">
          {error}
        </div>
      ) : null}

      {success ? (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:border-emerald-900/60 dark:bg-emerald-950/40 dark:text-emerald-200">
          {success}
        </div>
      ) : null}

      <Card title="Metas">
        {loading ? (
          <div className="text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div>
        ) : (
          <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
            <label className="flex flex-col gap-1">
              <span className="text-xs font-medium text-zinc-700 dark:text-zinc-300">Fila (Queued)</span>
              <input
                className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                value={queued}
                onChange={(e) => setQueued(e.target.value)}
                disabled={!canManage || saving}
              />
            </label>
            <label className="flex flex-col gap-1">
              <span className="text-xs font-medium text-zinc-700 dark:text-zinc-300">Preparo (base)</span>
              <input
                className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                value={prep}
                onChange={(e) => setPrep(e.target.value)}
                disabled={!canManage || saving}
              />
            </label>
            <label className="flex flex-col gap-1">
              <span className="text-xs font-medium text-zinc-700 dark:text-zinc-300">Pronto (Ready)</span>
              <input
                className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                value={ready}
                onChange={(e) => setReady(e.target.value)}
                disabled={!canManage || saving}
              />
            </label>
          </div>
        )}

        <div className="mt-4 flex justify-end">
          <button
            className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
            onClick={onSave}
            disabled={!canManage || saving}
          >
            {saving ? "Salvando…" : "Salvar"}
          </button>
        </div>

        {!canManage ? (
          <div className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">
            Requer `kitchen_sla:manage`.
          </div>
        ) : null}
      </Card>
    </div>
  );
}

