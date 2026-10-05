import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

type EmptyStateProps = {
  title: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
  className?: string;
};

/**
 * Message for a section that has nothing to show.
 * An icon, description, and action appear only when the caller passes them.
 */
function EmptyState({
  title,
  description,
  icon,
  action,
  className,
}: EmptyStateProps) {
  return (
    <div
      data-slot="empty-state"
      className={cn(
        "flex flex-col items-center justify-center gap-3 px-4 py-14 text-center",
        className,
      )}
    >
      {icon}
      <p className="text-xl font-semibold leading-none text-foreground">
        {title}
      </p>
      {description ? (
        <p className="max-w-sm text-sm text-muted-foreground">{description}</p>
      ) : null}
      {action ? <div className="mt-3">{action}</div> : null}
    </div>
  );
}

export { EmptyState };
export type { EmptyStateProps };
