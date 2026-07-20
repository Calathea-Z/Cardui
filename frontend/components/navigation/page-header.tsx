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
            <p className="text-sm text-muted-foreground">{eyebrow}</p>
          ) : null}
          <h1 className="text-3xl font-semibold text-foreground">{title}</h1>
          {description ? (
            <p className="mt-1 text-sm text-muted-foreground">{description}</p>
          ) : null}
        </div>
      </div>

      {actions ? <div className="w-full sm:w-auto">{actions}</div> : null}
    </div>
  );
}
