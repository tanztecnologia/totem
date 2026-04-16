import { ReactNode } from "react";

export function Card(props: { title?: string; children: ReactNode; className?: string }) {
  return (
    <section
      className={[
        "rounded-2xl border border-zinc-200 bg-white p-4 shadow-sm dark:border-zinc-800 dark:bg-zinc-950",
        props.className ?? "",
      ].join(" ")}
    >
      {props.title ? (
        <div className="mb-3 text-sm font-semibold text-zinc-950 dark:text-zinc-50">
          {props.title}
        </div>
      ) : null}
      {props.children}
    </section>
  );
}

