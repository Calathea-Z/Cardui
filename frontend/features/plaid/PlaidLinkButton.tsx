"use client";

import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
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
      <Button
        type="button"
        disabled={!canAttemptConnect}
        onClick={() => void open()}
        size="lg"
        className="w-fit"
      >
        {isCreatingToken
          ? "Preparing Plaid"
          : isExchangingToken
            ? "Connecting"
            : errorMessage
              ? "Try again"
              : "Connect account"}
      </Button>

      {errorMessage ? (
        <Alert variant="destructive">
          {errorMessage}{" "}
          <button
            type="button"
            onClick={clearError}
            className="cursor-pointer underline underline-offset-2"
          >
            Dismiss
          </button>
        </Alert>
      ) : null}
    </div>
  );
}
