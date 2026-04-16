import Link from "next/link";

export default function Home() {
  return (
    <div className="flex flex-col flex-1 items-center justify-center bg-zinc-50 font-sans dark:bg-black">
      <main className="flex w-full max-w-xl flex-col items-stretch gap-4 rounded-2xl bg-white p-8 shadow-sm dark:bg-zinc-950">
        <h1 className="text-2xl font-semibold tracking-tight text-zinc-950 dark:text-zinc-50">
          Totem Admin
        </h1>
        <p className="text-sm text-zinc-600 dark:text-zinc-400">
          Acesse o painel administrativo.
        </p>
        <div className="flex gap-3">
          <Link
            className="inline-flex h-10 items-center justify-center rounded-lg bg-zinc-900 px-4 text-sm font-medium text-white hover:bg-zinc-800 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
            href="/login"
          >
            Login
          </Link>
          <Link
            className="inline-flex h-10 items-center justify-center rounded-lg border border-zinc-200 px-4 text-sm font-medium text-zinc-900 hover:bg-zinc-50 dark:border-zinc-800 dark:text-zinc-50 dark:hover:bg-zinc-900"
            href="/dashboard"
          >
            Dashboard
          </Link>
        </div>
      </main>
    </div>
  );
}
