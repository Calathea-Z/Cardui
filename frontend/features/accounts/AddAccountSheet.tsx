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

/**
 * Blank values for a new manual account.
 * The type starts as a depository account, and the opening date is today in the local calendar.
 */
const emptyForm = (): ManualAccountFormValues => ({
  name: "",
  type: "depository",
  subtype: "",
  openingBalance: "",
  openingBalanceDate: todayDateInput(),
});

/**
 * Lets the household enter an account by hand or link one through Plaid.
 * Plaid starts only after that choice, and a successful link closes the sheet and reloads the page.
 */
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

  /**
   * Creates a manual account from the form and reloads the page.
   * A blank name, invalid opening balance, or missing opening date shows an error and skips the request.
   */
  async function saveManualAccount() {
    const openingBalance = parseMoney(form.openingBalance);
    if (
      !form.name.trim() ||
      openingBalance === null ||
      !form.openingBalanceDate
    ) {
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
      <Button
        type="button"
        size="lg"
        className="py-3"
        onClick={() => setMode("manual")}
      >
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

/**
 * Sheet for adding a manual account or linking one through Plaid.
 * Escape closes the sheet while the type list or the date calendar is closed, and reopening starts a fresh form.
 */
export function AddAccountSheet({ open, onClose }: AddAccountSheetProps) {
  const [pickerOpen, setPickerOpen] = useState(false);

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title="Add account"
      closeOnEscape={!pickerOpen}
      presentation="panel"
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
