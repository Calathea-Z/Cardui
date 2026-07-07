"use client";

import { useRouter } from "next/navigation";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { usePlaidLinkFlow } from "@/features/plaid/usePlaidLinkFlow";

type AddAccountSheetProps = {
  open: boolean;
  onClose: () => void;
};

export function AddAccountSheet({ open, onClose }: AddAccountSheetProps) {
  const router = useRouter();

  const {
    open: openPlaid,
    canAttemptConnect,
    isCreatingToken,
    isExchangingToken,
    errorMessage,
    clearError,
  } = usePlaidLinkFlow({
    onSuccess: () => {
      onClose();
      router.refresh();
    },
  });

  const buttonLabel = isCreatingToken
    ? "Preparing Plaid"
    : isExchangingToken
      ? "Connecting"
      : errorMessage
        ? "Try again"
        : "Connect with Plaid";

  return (
    <BottomSheet open={open} onClose={onClose} title="Connect account">
      <div className="flex flex-col gap-4">
        <p className="text-sm text-muted-foreground">
          Securely link a bank or investment account through Plaid. Your
          credentials are never stored by Cardui.
        </p>

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

        <button
          type="button"
          disabled={!canAttemptConnect}
          onClick={() => openPlaid()}
          className="app-cta-button py-3"
        >
          {buttonLabel}
        </button>
      </div>
    </BottomSheet>
  );
}
