"use client";

import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "../../../components/page_header";
import { Card } from "../../../components/card";
import { errorMessage } from "../../../lib/api";
import { createUser, getMe } from "../../lib/admin";
import { readSession, hasPermission } from "../../../lib/auth";

export default function UsersAdminPage() {
  const session = useMemo(() => readSession(), []);
  const canManage = !!session && hasPermission(session, "users:manage");

  const [me, setMe] = useState<{ email: string; role?: string | null; permissions: string[] } | null>(null);
  const [loadingMe, setLoadingMe] = useState(false);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState("Staff");

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function run() {
      setLoadingMe(true);
      try {
        const resp = await getMe();
        if (cancelled) return;
        setMe({ email: resp.email, role: resp.role, permissions: resp.permissions ?? [] });
      } catch {
        if (cancelled) return;
        setMe(null);
      } finally {
        if (!cancelled) setLoadingMe(false);
      }
    }
    run();
    return () => {
      cancelled = true;
    };
  }, []);

  async function onCreate() {
    if (!canManage) {
      setError("Sem permissão para gerenciar usuários.");
      return;
    }

    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      setError("Informe o email.");
      return;
    }
    if (password.trim().length < 3) {
      setError("Senha inválida.");
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);
    try {
      const resp = await createUser({ email: trimmedEmail, password, role });
      setSuccess(`Usuário criado: ${resp.email} (${resp.role})`);
      setEmail("");
      setPassword("");
      setRole("Staff");
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Usuários e ACL"
        subtitle="Criação de usuários (permissões hoje derivam do role)."
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

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        <Card title="Meu usuário">
          {loadingMe ? (
            <div className="text-sm text-zinc-600 dark:text-zinc-400">Carregando…</div>
          ) : me ? (
            <div className="flex flex-col gap-2 text-sm">
              <div className="text-zinc-600 dark:text-zinc-400">Email</div>
              <div className="font-medium text-zinc-950 dark:text-zinc-50">{me.email}</div>
              <div className="text-zinc-600 dark:text-zinc-400">Role</div>
              <div className="font-medium text-zinc-950 dark:text-zinc-50">{me.role ?? "—"}</div>
              <div className="text-zinc-600 dark:text-zinc-400">Permissões</div>
              <div className="flex flex-wrap gap-2">
                {me.permissions.map((p) => (
                  <span
                    key={p}
                    className="rounded-full border border-zinc-200 bg-zinc-50 px-3 py-1 text-xs font-medium text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-200"
                  >
                    {p}
                  </span>
                ))}
              </div>
            </div>
          ) : (
            <div className="text-sm text-zinc-600 dark:text-zinc-400">Não foi possível carregar.</div>
          )}
        </Card>

        <Card title="Criar usuário">
          <div className="flex flex-col gap-3">
            <div className="text-sm text-zinc-600 dark:text-zinc-400">
              Requer `users:manage`.
            </div>
            <input
              className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Email"
              disabled={!canManage || saving}
            />
            <input
              className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Senha"
              type="password"
              disabled={!canManage || saving}
            />
            <select
              className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              value={role}
              onChange={(e) => setRole(e.target.value)}
              disabled={!canManage || saving}
            >
              <option value="Admin">Admin</option>
              <option value="Staff">Staff</option>
              <option value="Totem">Totem</option>
              <option value="Waiter">Waiter</option>
              <option value="Pdv">Pdv</option>
            </select>
            <button
              className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
              onClick={onCreate}
              disabled={!canManage || saving}
            >
              {saving ? "Salvando…" : "Criar"}
            </button>
          </div>
        </Card>
      </div>
    </div>
  );
}

