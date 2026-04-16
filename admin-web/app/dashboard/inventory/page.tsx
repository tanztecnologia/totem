"use client";

import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "../../components/page_header";
import { Card } from "../../components/card";
import { errorMessage } from "../../lib/api";
import {
  addSkuStockEntry,
  getSkuByCode,
  listSkuStockConsumptions,
  replaceSkuStockConsumptions,
  type SkuResult,
  type SkuStockConsumption,
} from "../lib/catalog";
import { formatMoney } from "../lib/dashboard";
import { readSession, hasPermission } from "../../lib/auth";

type ConsumptionDraft = { sourceSkuCode: string; quantity: string; unit: string };

export default function InventoryPage() {
  const session = useMemo(() => readSession(), []);
  const canWrite = !!session && hasPermission(session, "catalog:write");

  const [code, setCode] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sku, setSku] = useState<SkuResult | null>(null);

  const [consumptions, setConsumptions] = useState<SkuStockConsumption[]>([]);
  const [loadingConsumptions, setLoadingConsumptions] = useState(false);

  const [entryQty, setEntryQty] = useState("");
  const [entryUnit, setEntryUnit] = useState("kg");
  const [savingEntry, setSavingEntry] = useState(false);

  const [drafts, setDrafts] = useState<ConsumptionDraft[]>([
    { sourceSkuCode: "", quantity: "", unit: "g" },
  ]);
  const [savingConsumptions, setSavingConsumptions] = useState(false);

  async function load() {
    const trimmed = code.trim();
    if (!trimmed) {
      setError("Informe o código do SKU.");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const resp = await getSkuByCode(trimmed);
      setSku(resp);
      await loadConsumptions(resp.id);
    } catch (e) {
      setError(errorMessage(e));
      setSku(null);
      setConsumptions([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadConsumptions(skuId: string) {
    setLoadingConsumptions(true);
    try {
      const list = await listSkuStockConsumptions(skuId);
      setConsumptions(list);
    } catch {
      setConsumptions([]);
    } finally {
      setLoadingConsumptions(false);
    }
  }

  const entryUnitOptions = useMemo(() => {
    const base = sku?.stockBaseUnit;
    const normalized = typeof base === "number" ? base : base;
    if (normalized === 0 || normalized === "Unit") return ["un"];
    if (normalized === 1 || normalized === "Gram") return ["g", "kg"];
    if (normalized === 2 || normalized === "Milliliter") return ["ml", "l"];
    return ["g", "kg", "ml", "l", "un"];
  }, [sku?.stockBaseUnit]);

  useEffect(() => {
    if (!entryUnitOptions.includes(entryUnit)) {
      setEntryUnit(entryUnitOptions[0] ?? "g");
    }
  }, [entryUnit, entryUnitOptions]);

  async function addEntry() {
    if (!sku) return;
    if (!canWrite) {
      setError("Sem permissão para alterar estoque.");
      return;
    }
    if (!sku.tracksStock) {
      setError("Este SKU não controla estoque próprio. Configure estoque no SKU base ou use receita (consumo por venda).");
      return;
    }
    const qty = Number(entryQty.replace(",", "."));
    if (!Number.isFinite(qty) || qty <= 0) {
      setError("Entrada inválida.");
      return;
    }
    const unit = entryUnit.trim();
    if (!unit) {
      setError("Unidade inválida.");
      return;
    }

    setSavingEntry(true);
    setError(null);
    try {
      const updated = await addSkuStockEntry({ skuId: sku.id, quantity: qty, unit });
      setSku(updated);
      setEntryQty("");
      await loadConsumptions(updated.id);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSavingEntry(false);
    }
  }

  async function saveConsumptions() {
    if (!sku) return;
    if (!canWrite) {
      setError("Sem permissão para alterar consumo por venda.");
      return;
    }

    const items = drafts
      .map((d) => ({
        sourceSkuCode: d.sourceSkuCode.trim(),
        quantity: Number(d.quantity.replace(",", ".")),
        unit: d.unit.trim(),
      }))
      .filter((x) => x.sourceSkuCode.length > 0);

    if (items.length === 0) {
      setError("Informe pelo menos um SKU base.");
      return;
    }
    if (items.some((x) => !Number.isFinite(x.quantity) || x.quantity <= 0)) {
      setError("Quantidade inválida no consumo.");
      return;
    }
    if (items.some((x) => x.unit.length === 0)) {
      setError("Unidade inválida no consumo.");
      return;
    }

    setSavingConsumptions(true);
    setError(null);
    try {
      const saved = await replaceSkuStockConsumptions({ skuId: sku.id, items });
      setConsumptions(saved);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSavingConsumptions(false);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Estoque"
        subtitle="Entradas e consumo por venda (porção consome insumo)."
      />

      <Card>
        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <input
            className="h-10 flex-1 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            placeholder="Código do SKU (ex.: 00010)"
          />
          <button
            className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
            onClick={load}
            disabled={loading}
          >
            {loading ? "Buscando…" : "Buscar"}
          </button>
        </div>
      </Card>

      {error ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-200">
          {error}
        </div>
      ) : null}

      {sku ? (
        <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
          <Card title="SKU">
            <div className="grid grid-cols-2 gap-2 text-sm">
              <div className="text-zinc-600 dark:text-zinc-400">Código</div>
              <div className="font-medium text-zinc-950 dark:text-zinc-50">{sku.code}</div>
              <div className="text-zinc-600 dark:text-zinc-400">Nome</div>
              <div className="font-medium text-zinc-950 dark:text-zinc-50">{sku.name}</div>
              <div className="text-zinc-600 dark:text-zinc-400">Preço</div>
              <div className="font-medium text-zinc-950 dark:text-zinc-50">{formatMoney(sku.priceCents)}</div>
              <div className="text-zinc-600 dark:text-zinc-400">Unidade base</div>
              <div className="font-medium text-zinc-950 dark:text-zinc-50">
                {String(sku.stockBaseUnit ?? "—")}
              </div>
              <div className="text-zinc-600 dark:text-zinc-400">Estoque próprio</div>
              <div className="font-medium text-zinc-950 dark:text-zinc-50">
                {sku.tracksStock ? "Sim" : "Não"}
              </div>
              <div className="text-zinc-600 dark:text-zinc-400">Saldo</div>
              <div className="font-medium text-zinc-950 dark:text-zinc-50">
                {sku.stockOnHandBaseQty ?? "—"}
              </div>
            </div>
          </Card>

          <Card title="Entrada de estoque">
            <div className="flex flex-col gap-3">
              <div className="text-sm text-zinc-600 dark:text-zinc-400">
                Requer `catalog:write` e o SKU deve ter controle de estoque configurado.
              </div>
              <div className="flex gap-2">
                <input
                  className="h-10 flex-1 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                  value={entryQty}
                  onChange={(e) => setEntryQty(e.target.value)}
                  placeholder="Quantidade (ex.: 10)"
                  disabled={!canWrite || savingEntry}
                />
                <select
                  className="h-10 w-28 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                  value={entryUnit}
                  onChange={(e) => setEntryUnit(e.target.value)}
                  disabled={!canWrite || savingEntry}
                >
                  {entryUnitOptions.map((u) => (
                    <option key={u} value={u}>
                      {u}
                    </option>
                  ))}
                </select>
                <button
                  className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
                  onClick={addEntry}
                  disabled={!canWrite || savingEntry}
                >
                  {savingEntry ? "Salvando…" : "Adicionar"}
                </button>
              </div>
            </div>
          </Card>
        </div>
      ) : null}

      {sku ? (
        <Card title="Consumo por venda (receita)">
          <div className="text-sm text-zinc-600 dark:text-zinc-400">
            Ex.: Porção 200g consome 200g do SKU base (batata). Na venda, o sistema dá baixa automaticamente.
          </div>

          <div className="mt-4 flex flex-col gap-2">
            {drafts.map((d, idx) => (
              <div key={idx} className="flex flex-col gap-2 md:flex-row md:items-center">
                <input
                  className="h-10 flex-1 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                  value={d.sourceSkuCode}
                  onChange={(e) =>
                    setDrafts((prev) =>
                      prev.map((x, i) => (i === idx ? { ...x, sourceSkuCode: e.target.value } : x)),
                    )
                  }
                  placeholder="SKU base (código)"
                  disabled={!canWrite || savingConsumptions}
                />
                <input
                  className="h-10 w-40 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                  value={d.quantity}
                  onChange={(e) =>
                    setDrafts((prev) =>
                      prev.map((x, i) => (i === idx ? { ...x, quantity: e.target.value } : x)),
                    )
                  }
                  placeholder="Qtd"
                  disabled={!canWrite || savingConsumptions}
                />
                <select
                  className="h-10 w-28 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                  value={d.unit}
                  onChange={(e) =>
                    setDrafts((prev) =>
                      prev.map((x, i) => (i === idx ? { ...x, unit: e.target.value } : x)),
                    )
                  }
                  disabled={!canWrite || savingConsumptions}
                >
                  <option value="un">un</option>
                  <option value="g">g</option>
                  <option value="kg">kg</option>
                  <option value="ml">ml</option>
                  <option value="l">l</option>
                </select>
                <button
                  className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 bg-white px-3 text-sm font-medium text-zinc-900 hover:bg-zinc-50 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
                  onClick={() => setDrafts((prev) => prev.filter((_, i) => i !== idx))}
                  disabled={!canWrite || savingConsumptions || drafts.length <= 1}
                >
                  Remover
                </button>
              </div>
            ))}
            <div className="flex gap-2">
              <button
                className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 bg-white px-4 text-sm font-medium text-zinc-900 hover:bg-zinc-50 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
                onClick={() => setDrafts((prev) => [...prev, { sourceSkuCode: "", quantity: "", unit: "g" }])}
                disabled={!canWrite || savingConsumptions}
              >
                + Adicionar item
              </button>
              <button
                className="ml-auto inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
                onClick={saveConsumptions}
                disabled={!canWrite || savingConsumptions}
              >
                {savingConsumptions ? "Salvando…" : "Salvar receita"}
              </button>
            </div>
          </div>

          <div className="mt-6">
            <div className="text-sm font-semibold text-zinc-950 dark:text-zinc-50">Receita atual</div>
            {loadingConsumptions ? (
              <div className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div>
            ) : consumptions.length === 0 ? (
              <div className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">Sem consumo cadastrado.</div>
            ) : (
              <div className="mt-3 overflow-hidden rounded-xl border border-zinc-200 dark:border-zinc-800">
                <div className="grid grid-cols-12 gap-2 border-b border-zinc-200 bg-zinc-50 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
                  <div className="col-span-4">SKU base</div>
                  <div className="col-span-8">Qtd (base)</div>
                </div>
                {consumptions.map((c) => (
                  <div
                    key={c.id}
                    className="grid grid-cols-12 gap-2 border-b border-zinc-100 px-3 py-2 text-sm text-zinc-900 last:border-b-0 dark:border-zinc-900 dark:text-zinc-50"
                  >
                    <div className="col-span-4 font-medium">
                      {c.sourceSkuCode?.trim() ? c.sourceSkuCode : c.sourceSkuId}
                    </div>
                    <div className="col-span-8">{c.quantityBase}</div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Card>
      ) : null}

      {!canWrite ? (
        <div className="text-xs text-zinc-600 dark:text-zinc-400">
          Seu usuário não tem `catalog:write`, então apenas leitura.
        </div>
      ) : null}
    </div>
  );
}
