import { ReactNode, useEffect } from "react";

export function Modal(props: {
  open: boolean;
  title: string;
  children: ReactNode;
  onClose: () => void;
  footer?: ReactNode;
  maxWidthClassName?: string;
}) {
  useEffect(() => {
    if (!props.open) return;
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") props.onClose();
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [props]);

  if (!props.open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div
        className={[
          "w-full rounded-2xl border border-zinc-200 bg-white shadow-xl dark:border-zinc-800 dark:bg-zinc-950",
          props.maxWidthClassName ?? "max-w-2xl",
        ].join(" ")}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-zinc-200 px-5 py-4 dark:border-zinc-800">
          <div className="text-sm font-semibold text-zinc-950 dark:text-zinc-50">
            {props.title}
          </div>
          <button
            className="rounded-lg px-2 py-1 text-sm font-medium text-zinc-700 hover:bg-zinc-100 dark:text-zinc-200 dark:hover:bg-zinc-900"
            onClick={props.onClose}
          >
            Fechar
          </button>
        </div>
        <div className="px-5 py-4">{props.children}</div>
        {props.footer ? (
          <div className="flex items-center justify-end gap-2 border-t border-zinc-200 px-5 py-4 dark:border-zinc-800">
            {props.footer}
          </div>
        ) : null}
      </div>
    </div>
  );
}

