"use client";

import { useEffect, useState } from "react";
import { PageHeader } from "../../../components/page_header";
import { Card } from "../../../components/card";
import { errorMessage } from "../../../lib/api";
import { createCategory, listCategories, type CategoryResult } from "../../lib/catalog";

export default function AdminCategoriesPage() {
  const [includeInactive, setIncludeInactive] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [items, setItems] = useState<CategoryResult[]>([]);

  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [creating, setCreating] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const resp = await listCategories({ includeInactive });
      setItems(resp);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setLoading(false);
    }
  }

  async function onCreate() {
    if (creating) return;
    const trimmedName = name.trim();
    if (trimmedName.length < 2) {
      setError("Nome inválido.");
      return;
    }
    setCreating(true);
    setError(null);
    try {
      await createCategory({
        name: trimmedName,
        slug: slug.trim().length > 0 ? slug.trim() : null,
        isActive,
      });
      setName("");
      setSlug("");
      setIsActive(true);
      await load();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setCreating(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Categorias"
        subtitle="Crie e organize categorias (código sequencial)."
        actions={
          <button
            className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
            onClick={load}
            disabled={loading}
          >
            Atualizar
          </button>
        }
      />

      {error ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-200">
          {error}
        </div>
      ) : null}

      <Card title="Criar categoria">
        <div className="flex flex-col gap-3 md:flex-row">
          <input
            className="h-10 flex-1 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Nome"
          />
          <input
            className="h-10 flex-1 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
            value={slug}
            onChange={(e) => setSlug(e.target.value)}
            placeholder="Slug (opcional)"
          />
          <label className="flex items-center gap-2 text-sm text-zinc-700 dark:text-zinc-200">
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
            Ativa
          </label>
          <button
            className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 bg-white px-4 text-sm font-medium text-zinc-900 hover:bg-zinc-50 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
            onClick={onCreate}
            disabled={creating}
          >
            {creating ? "Salvando…" : "Criar"}
          </button>
        </div>
      </Card>

      <Card title="Lista">
        <div className="mb-3 flex items-center gap-2 text-sm text-zinc-700 dark:text-zinc-200">
          <input
            type="checkbox"
            checked={includeInactive}
            onChange={(e) => setIncludeInactive(e.target.checked)}
          />
          <span>Incluir inativas</span>
          <button
            className="ml-auto inline-flex h-9 items-center justify-center rounded-lg border border-zinc-200 bg-white px-3 text-sm font-medium text-zinc-900 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
            onClick={load}
            disabled={loading}
          >
            Aplicar
          </button>
        </div>

        {loading ? <div className="text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div> : null}
        {!loading && items.length === 0 ? (
          <div className="text-sm text-zinc-600 dark:text-zinc-400">Sem categorias</div>
        ) : null}
        {items.length > 0 ? (
          <div className="overflow-hidden rounded-xl border border-zinc-200 dark:border-zinc-800">
            <div className="grid grid-cols-12 gap-2 border-b border-zinc-200 bg-zinc-50 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
              <div className="col-span-2">Código</div>
              <div className="col-span-4">Nome</div>
              <div className="col-span-4">Slug</div>
              <div className="col-span-2 text-right">Ativa</div>
            </div>
            {items.map((c) => (
              <div
                key={c.id}
                className="grid grid-cols-12 gap-2 border-b border-zinc-100 px-3 py-2 text-sm text-zinc-900 last:border-b-0 dark:border-zinc-900 dark:text-zinc-50"
              >
                <div className="col-span-2 font-medium">{c.code}</div>
                <div className="col-span-4 truncate">{c.name}</div>
                <div className="col-span-4 truncate">{c.slug}</div>
                <div className="col-span-2 text-right">{c.isActive ? "Sim" : "Não"}</div>
              </div>
            ))}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

