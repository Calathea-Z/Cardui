"use client";

import { usePlaidLinkFlow } from "./usePlaidLinkFlow";

export function PlaidLinkButton() {
  const { open, isReady, isCreatingToken, isExchangingToken } =
    usePlaidLinkFlow();

  return (
    <button
      type="button"
      disabled={!isReady}
      onClick={() => open()}
      className="app-cta-button"
    >
      {isCreatingToken
        ? "Preparing Plaid"
        : isExchangingToken
          ? "Connecting"
          : "Connect account"}
    </button>
  );
}
