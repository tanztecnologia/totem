import { ReactNode } from "react";

export function PageHeader(props: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
      <div>
        <h1 className="text-xl font-semibold tracking-tight text-zinc-950 dark:text-zinc-50">
          {props.title}
        </h1>
        {props.subtitle ? (
          <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            {props.subtitle}
          </p>
        ) : null}
      </div>
      {props.actions ? <div className="flex gap-2">{props.actions}</div> : null}
    </div>
  );
}

