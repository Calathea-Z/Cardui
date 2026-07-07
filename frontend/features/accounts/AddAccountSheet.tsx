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

  const { open: openPlaid, isReady, isCreatingToken, isExchangingToken } =
    usePlaidLinkFlow({
      onSuccess: () => {
        onClose();
        router.refresh();
      },
    });

  const buttonLabel = isCreatingToken
    ? "Preparing Plaid"
    : isExchangingToken
      ? "Connecting"
      : "Connect with Plaid";

  return (
    <BottomSheet open={open} onClose={onClose} title="Connect account">
      <div className="flex flex-col gap-4">
        <p className="text-sm text-muted-foreground">
          Securely link a bank or investment account through Plaid. Your
          credentials are never stored by Cardui.
        </p>

        <button
          type="button"
          disabled={!isReady}
          onClick={() => openPlaid()}
          className="app-cta-button py-3"
        >
          {buttonLabel}
        </button>
      </div>
    </BottomSheet>
  );
}
