"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import type { HouseholdContributorDto } from "@/lib/api/types";

type ContributorRowProps = {
  contributor: HouseholdContributorDto;
  onChange: (contributor: HouseholdContributorDto) => void;
  onSave: () => void;
  onRemove: () => void;
};

/**
 * Edits one contributor's name and whether they are shown.
 * Name and visibility are saved together, and Remove deletes that contributor.
 */
export function ContributorRow({
  contributor,
  onChange,
  onSave,
  onRemove,
}: ContributorRowProps) {
  return (
    <li className="flex flex-col gap-3 rounded-lg border border-border/70 p-3 sm:flex-row sm:items-end">
      <label className="flex min-w-0 flex-1 flex-col gap-1.5 text-sm font-medium">
        Name
        <Input
          value={contributor.name}
          onChange={(event) =>
            onChange({ ...contributor, name: event.target.value })
          }
          maxLength={80}
          autoComplete="off"
        />
      </label>
      <div className="sm:w-36">
        <Switch
          checked={contributor.isVisible}
          onCheckedChange={(checked) =>
            onChange({ ...contributor, isVisible: checked })
          }
          label="Shown"
        />
      </div>
      <div className="flex gap-2">
        <Button type="button" variant="outline" onClick={onSave}>
          Save
        </Button>
        <Button type="button" variant="ghost" onClick={onRemove}>
          Remove
        </Button>
      </div>
    </li>
  );
}
