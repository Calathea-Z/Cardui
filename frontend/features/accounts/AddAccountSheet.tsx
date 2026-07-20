"use client";

import { useRouter } from "next/navigation";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { usePlaidLinkFlow } from "@/features/plaid/usePlaidLinkFlow";

type AddAccountSheetProps = {
  open: boolean;
  onClose: () => void;
};

function AddAccountSheetContent({ onClose }: { onClose: () => void }) {
  const router = useRouter();

  const {
    open: openPlaid,
    canAttemptConnect,
    isCreatingToken,
    isExchangingToken,
    errorMessage,
    clearError,
  } = usePlaidLinkFlow({
    enabled: true,
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
    <div className="flex flex-col gap-4">
      <p className="text-sm text-muted-foreground">
        Securely link a bank or investment account through Plaid. Your
        credentials are never stored by Cardui.
      </p>

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

      <Button
        type="button"
        disabled={!canAttemptConnect}
        onClick={() => void openPlaid()}
        size="lg"
        className="py-3"
      >
        {buttonLabel}
      </Button>
    </div>
  );
}

export function AddAccountSheet({ open, onClose }: AddAccountSheetProps) {
  return (
    <BottomSheet open={open} onClose={onClose} title="Connect account">
      {open ? <AddAccountSheetContent onClose={onClose} /> : null}
    </BottomSheet>
  );
}
