"use client";

import { cn } from "@/lib/utils";
import { usePlaidLinkFlow } from "./usePlaidLinkFlow";

type PlaidLinkButtonProps = {
  onSuccess?: () => void;
  className?: string;
};

export function PlaidLinkButton({
  onSuccess,
  className,
}: PlaidLinkButtonProps = {}) {
  const {
    open,
    canAttemptConnect,
    isCreatingToken,
    isExchangingToken,
    errorMessage,
    clearError,
  } = usePlaidLinkFlow({ onSuccess });

  return (
    <div className={cn("flex flex-col gap-3", className)}>
      <button
        type="button"
        disabled={!canAttemptConnect}
        onClick={() => void open()}
        className="app-cta-button w-fit"
      >
        {isCreatingToken
          ? "Preparing Plaid"
          : isExchangingToken
            ? "Connecting"
            : errorMessage
              ? "Try again"
              : "Connect account"}
      </button>

      {errorMessage ? (
        <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {errorMessage}{" "}
          <button
            type="button"
            onClick={clearError}
            className="cursor-pointer underline underline-offset-2"
          >
            Dismiss
          </button>
        </p>
      ) : null}
    </div>
  );
}
