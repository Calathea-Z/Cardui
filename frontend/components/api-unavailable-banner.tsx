"use client";

import { useRouter } from "next/navigation";
import { useTransition } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type ApiUnavailableBannerProps = {
  message?: string | null;
  className?: string;
};

/**
 * Banner that says the API cannot be reached.
 * Retry refreshes the current route, and a missing message says the backend may be offline, browsing still works, and data loads when the API is back.
 */
export function ApiUnavailableBanner({
  message,
  className,
}: ApiUnavailableBannerProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  return (
    <div
      role="alert"
      className={cn(
        "app-panel flex flex-col gap-3 border-destructive/40 bg-destructive/10 px-4 py-3 sm:flex-row sm:items-center sm:justify-between",
        className,
      )}
    >
      <div className="min-w-0">
        <p className="text-sm font-medium text-foreground">
          Can&apos;t reach the API
        </p>
        <p className="mt-0.5 text-sm text-muted-foreground">
          {message ??
            "The backend may be offline. You can still browse the app; data will load when it comes back."}
        </p>
      </div>

      <Button
        type="button"
        size="lg"
        className="shrink-0"
        disabled={isPending}
        onClick={() => {
          startTransition(() => {
            router.refresh();
          });
        }}
      >
        {isPending ? "Retrying…" : "Retry"}
      </Button>
    </div>
  );
}
