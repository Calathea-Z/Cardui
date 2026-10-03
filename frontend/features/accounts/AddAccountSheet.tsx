"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { createManualAccount } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import { usePlaidLinkFlow } from "@/features/plaid/usePlaidLinkFlow";
import {
  ManualAccountForm,
  type ManualAccountFormValues,
} from "./ManualAccountForm";
import { parseMoney, todayDateInput } from "./manualAccount";

type AddAccountSheetProps = {
  open: boolean;
  onClose: () => void;
};

type AddMode = "choose" | "manual" | "plaid";

const emptyForm = (): ManualAccountFormValues => ({
  name: "",
  type: "depository",
  subtype: "",
  openingBalance: "",
  openingBalanceDate: todayDateInput(),
});

function AddAccountSheetContent({
  onClose,
  onPickerOpenChange,
}: {
  onClose: () => void;
  onPickerOpenChange: (open: boolean) => void;
}) {
  const router = useRouter();
  const [mode, setMode] = useState<AddMode>("choose");
  const [form, setForm] = useState<ManualAccountFormValues>(emptyForm);
  const [manualError, setManualError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const {
    open: openPlaid,
    canAttemptConnect,
    isCreatingToken,
    isExchangingToken,
    errorMessage,
    clearError,
  } = usePlaidLinkFlow({
    enabled: mode === "plaid",
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

  async function saveManualAccount() {
    const openingBalance = parseMoney(form.openingBalance);
    if (!form.name.trim() || openingBalance === null || !form.openingBalanceDate) {
      setManualError("Enter a name, opening balance, and opening date.");
      return;
    }

    setIsSaving(true);
    setManualError(null);
    try {
      await createManualAccount({
        name: form.name.trim(),
        type: form.type,
        subtype: form.subtype.trim() || null,
        openingBalance,
        openingBalanceDate: form.openingBalanceDate,
      });
      onClose();
      router.refresh();
    } catch (error) {
      setManualError(getApiErrorMessage(error, "Could not add this account."));
    } finally {
      setIsSaving(false);
    }
  }

  if (mode === "manual") {
    return (
      <div className="flex flex-col gap-4">
        <ManualAccountForm
          values={form}
          onChange={setForm}
          onSubmit={() => void saveManualAccount()}
          submitLabel="Add account"
          isSaving={isSaving}
          errorMessage={manualError}
          onPickerOpenChange={onPickerOpenChange}
        />
        <Button type="button" variant="ghost" onClick={() => setMode("choose")}>
          Back
        </Button>
      </div>
    );
  }

  if (mode === "plaid") {
    return (
      <div className="flex flex-col gap-4">
        <p className="text-sm text-muted-foreground">
          Securely link a bank or investment account through Plaid. Your
          credentials are never stored by Tortoise.
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
        <Button type="button" variant="ghost" onClick={() => setMode("choose")}>
          Back
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm text-muted-foreground">
        Enter an account yourself, or link one through Plaid.
      </p>
      <Button type="button" size="lg" className="py-3" onClick={() => setMode("manual")}>
        Enter manually
      </Button>
      <Button
        type="button"
        variant="outline"
        size="lg"
        className="py-3"
        onClick={() => setMode("plaid")}
      >
        Connect with Plaid
      </Button>
    </div>
  );
}

export function AddAccountSheet({ open, onClose }: AddAccountSheetProps) {
  const [pickerOpen, setPickerOpen] = useState(false);

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title="Add account"
      closeOnEscape={!pickerOpen}
    >
      {open ? (
        <AddAccountSheetContent
          onClose={onClose}
          onPickerOpenChange={setPickerOpen}
        />
      ) : null}
    </BottomSheet>
  );
}
