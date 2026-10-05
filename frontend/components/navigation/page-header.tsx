import type { ReactNode } from "react";
import { BackButton } from "./back-button";

type PageHeaderProps = {
  title: string;
  description?: string;
  backFallbackHref?: string;
  showBack?: boolean;
  actions?: ReactNode;
};

/**
 * Title row for a page, with optional description and actions.
 * Primary pages omit the back control. Actions wrap under the title on a narrow row.
 */
export function PageHeader({
  title,
  description,
  backFallbackHref = "/",
  showBack = false,
  actions,
}: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
      <div className="flex min-w-0 items-start gap-3">
        {showBack ? (
          <BackButton
            fallbackHref={backFallbackHref}
            className="mt-0.5 hidden md:inline-flex"
          />
        ) : null}

        <div className="min-w-0">
          <h1 className="text-[1.75rem] font-semibold leading-none tracking-[-0.02em] text-foreground">
            {title}
          </h1>
          {description ? (
            <p className="mt-2 max-w-xl text-sm text-muted-foreground">
              {description}
            </p>
          ) : null}
        </div>
      </div>

      {actions ? <div className="w-full sm:w-auto">{actions}</div> : null}
    </div>
  );
}
