"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { createSubGroup } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { SubGroupDto } from "@/lib/api/types";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";

type AddSubGroupDrawerProps = {
  open: boolean;
  groupId: string;
  groupName: string;
  onClose: () => void;
  onCreated: (subGroup: SubGroupDto) => void;
};

export function AddSubGroupDrawer({
  open,
  groupId,
  groupName,
  onClose,
  onCreated,
}: AddSubGroupDrawerProps) {
  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title="New Sub-group"
      headerAction="close"
      overlayClassName="z-80"
      className={FULL_SCREEN_SHEET_CLASSNAME}
    >
      <AddSubGroupDrawerContent
        key={groupId}
        groupId={groupId}
        groupName={groupName}
        onCreated={onCreated}
      />
    </BottomSheet>
  );
}

function AddSubGroupDrawerContent({
  groupId,
  groupName,
  onCreated,
}: {
  groupId: string;
  groupName: string;
  onCreated: (subGroup: SubGroupDto) => void;
}) {
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmed = name.trim();
    if (!trimmed || isSaving) {
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const subGroup = await createSubGroup({
        groupId,
        name: trimmed,
      });
      onCreated(subGroup);
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create sub-group."));
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <p className="text-sm text-muted-foreground">
        Adding under <span className="text-foreground">{groupName}</span>
      </p>

      <label className="block">
        <span className="text-sm text-muted-foreground">Name</span>
        <Input
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="mt-2 h-10"
          placeholder="Pets"
          autoFocus
          disabled={isSaving}
        />
      </label>

      {error ? <Alert variant="destructive">{error}</Alert> : null}

      <Button type="submit" disabled={isSaving || !name.trim()} size="lg">
        {isSaving ? "Creating" : "Create sub-group"}
      </Button>
    </form>
  );
}
