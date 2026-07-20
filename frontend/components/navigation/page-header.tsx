import type { ReactNode } from "react";
import { BackButton } from "./back-button";

type PageHeaderProps = {
  eyebrow?: string;
  title: string;
  description?: string;
  backFallbackHref?: string;
  actions?: ReactNode;
};

export function PageHeader({
  eyebrow,
  title,
  description,
  backFallbackHref = "/",
  actions,
}: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div className="flex items-start gap-3">
        <BackButton
          fallbackHref={backFallbackHref}
          className="mt-0.5 hidden md:inline-flex"
        />

        <div>
          {eyebrow ? (
            <p className="text-[11px] font-medium tracking-[0.18em] text-primary uppercase">
              {eyebrow}
            </p>
          ) : null}
          <h1 className="font-brand mt-1 text-[2.15rem] leading-none tracking-tight text-foreground">
            <span className="ink-underline">{title}</span>
          </h1>
          {description ? (
            <p className="mt-2.5 max-w-xl text-sm text-muted-foreground">
              {description}
            </p>
          ) : null}
        </div>
      </div>

      {actions ? <div className="w-full sm:w-auto">{actions}</div> : null}
    </div>
  );
}
