"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { errorMessage, login } from "../lib/api";
import { writeSession } from "../lib/auth";

export default function LoginPage() {
  const router = useRouter();
  const [tenantName, setTenantName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = useMemo(() => {
    return tenantName.trim().length > 0 && email.trim().length > 0 && password.trim().length > 0;
  }, [tenantName, email, password]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!canSubmit || isSubmitting) return;

    setIsSubmitting(true);
    setError(null);
    try {
      const resp = await login({ tenantName: tenantName.trim(), email: email.trim(), password });
      writeSession({
        tenantId: resp.tenantId,
        userId: resp.userId,
        email: resp.email,
        role: resp.role,
        permissions: resp.permissions ?? [],
        token: resp.token,
      });
      router.replace("/dashboard");
    } catch (e2) {
      setError(errorMessage(e2));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="flex flex-1 items-center justify-center bg-zinc-50 p-6 dark:bg-black">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-sm dark:bg-zinc-950">
        <h1 className="text-xl font-semibold tracking-tight text-zinc-950 dark:text-zinc-50">
          Login
        </h1>
        <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
          Use um usuário com permissão de dashboard.
        </p>

        <form className="mt-6 flex flex-col gap-4" onSubmit={onSubmit}>
          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Tenant</span>
            <input
              className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              value={tenantName}
              onChange={(e) => setTenantName(e.target.value)}
              disabled={isSubmitting}
              autoComplete="organization"
            />
          </label>

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Email</span>
            <input
              className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={isSubmitting}
              type="email"
              autoComplete="email"
            />
          </label>

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-zinc-900 dark:text-zinc-50">Senha</span>
            <input
              className="h-10 rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 outline-none focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isSubmitting}
              type="password"
              autoComplete="current-password"
            />
          </label>

          {error ? (
            <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-200">
              {error}
            </div>
          ) : null}

          <button
            className="mt-2 inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
            type="submit"
            disabled={!canSubmit || isSubmitting}
          >
            {isSubmitting ? "Entrando..." : "Entrar"}
          </button>
        </form>
      </div>
    </div>
  );
}
