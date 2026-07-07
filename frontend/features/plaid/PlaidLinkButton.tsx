"use client";

import { usePlaidLinkFlow } from "./usePlaidLinkFlow";

export function PlaidLinkButton() {
  const {
    open,
    canAttemptConnect,
    isCreatingToken,
    isExchangingToken,
    errorMessage,
    clearError,
  } = usePlaidLinkFlow();

  return (
    <div className="flex flex-col gap-3">
      <button
        type="button"
        disabled={!canAttemptConnect}
        onClick={() => open()}
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
