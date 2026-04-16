"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { clearSession, hasPermission, readSession } from "../lib/auth";

function NavItem(props: { href: string; label: string; active: boolean }) {
  return (
    <Link
      className={[
        "flex h-10 items-center rounded-lg px-3 text-sm font-medium",
        props.active
          ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900"
          : "text-zinc-700 hover:bg-zinc-100 dark:text-zinc-200 dark:hover:bg-zinc-900",
      ].join(" ")}
      href={props.href}
    >
      {props.label}
    </Link>
  );
}

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const session = readSession();

  useEffect(() => {
    const s = readSession();
    if (!s?.token) {
      router.replace("/login");
      return;
    }
    if (!hasPermission(s, "dashboard:read")) {
      router.replace("/login");
      return;
    }
  }, [router]);

  if (!session?.token || !hasPermission(session, "dashboard:read")) {
    return (
      <div className="flex flex-1 items-center justify-center bg-zinc-50 dark:bg-black">
        <div className="text-sm text-zinc-600 dark:text-zinc-400">Carregando...</div>
      </div>
    );
  }

  return (
    <div className="flex min-h-full flex-1 bg-zinc-50 dark:bg-black">
      <aside className="hidden w-64 flex-col border-r border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950 md:flex">
        <div className="mb-4">
          <div className="text-sm font-semibold text-zinc-950 dark:text-zinc-50">Totem Admin</div>
          <div className="mt-1 text-xs text-zinc-600 dark:text-zinc-400">
            {session?.email ?? ""}
          </div>
        </div>
        <nav className="flex flex-col gap-2">
          <NavItem href="/dashboard" label="Visão Geral" active={pathname === "/dashboard"} />
          <NavItem href="/dashboard/orders" label="Pedidos" active={pathname?.startsWith("/dashboard/orders") ?? false} />
          <NavItem href="/dashboard/inventory" label="Estoque" active={pathname?.startsWith("/dashboard/inventory") ?? false} />
          <div className="mt-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
            Catálogo
          </div>
          <NavItem
            href="/dashboard/catalog/skus"
            label="Produtos"
            active={pathname?.startsWith("/dashboard/catalog/skus") ?? false}
          />
          <NavItem
            href="/dashboard/catalog/categories"
            label="Categorias"
            active={pathname?.startsWith("/dashboard/catalog/categories") ?? false}
          />
          <div className="mt-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
            Configurações
          </div>
          <NavItem
            href="/dashboard/settings/users"
            label="Usuários"
            active={pathname?.startsWith("/dashboard/settings/users") ?? false}
          />
          <NavItem
            href="/dashboard/settings/kitchen-sla"
            label="Cozinha SLA"
            active={pathname?.startsWith("/dashboard/settings/kitchen-sla") ?? false}
          />
        </nav>
        <div className="mt-auto pt-4">
          <button
            className="inline-flex h-10 w-full items-center justify-center rounded-lg border border-zinc-200 bg-white px-3 text-sm font-medium text-zinc-900 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:hover:bg-zinc-900"
            onClick={() => {
              clearSession();
              router.replace("/login");
            }}
          >
            Sair
          </button>
        </div>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-zinc-200 bg-white px-4 py-3 dark:border-zinc-800 dark:bg-zinc-950 md:hidden">
          <div className="text-sm font-semibold text-zinc-950 dark:text-zinc-50">Totem Admin</div>
          <button
            className="text-sm font-medium text-zinc-700 hover:text-zinc-900 dark:text-zinc-200 dark:hover:text-white"
            onClick={() => {
              clearSession();
              router.replace("/login");
            }}
          >
            Sair
          </button>
        </header>
        <main className="flex flex-1 flex-col p-4 md:p-6">{children}</main>
      </div>
    </div>
  );
}
