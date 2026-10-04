"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { PageHeader } from "@/components/navigation/page-header";
import {
  addHouseholdContributor,
  removeHouseholdContributor,
  updateFinancialProfile,
  updateHouseholdContributor,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  FinancialProfileDto,
  HouseholdContributorDto,
} from "@/lib/api/types";
import { currencyChoices, timeZoneChoices } from "./profileOptions";

type HouseholdPageClientProps = {
  profile: FinancialProfileDto;
};

export function HouseholdPageClient({ profile }: HouseholdPageClientProps) {
  const router = useRouter();
  const [planningCurrency, setPlanningCurrency] = useState(
    profile.planningCurrency,
  );
  const [timeZoneId, setTimeZoneId] = useState(profile.timeZoneId);
  const [contributors, setContributors] = useState(profile.contributors);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [contributorError, setContributorError] = useState<string | null>(null);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [newName, setNewName] = useState("");
  const [newVisible, setNewVisible] = useState(true);
  const [isAdding, setIsAdding] = useState(false);

  const currencyOptions = useMemo(
    () => currencyChoices(planningCurrency),
    [planningCurrency],
  );
  const timeZoneOptions = useMemo(
    () => timeZoneChoices(timeZoneId),
    [timeZoneId],
  );

  async function saveProfile() {
    setIsSavingProfile(true);
    setProfileError(null);

    try {
      const saved = await updateFinancialProfile({
        planningCurrency,
        timeZoneId,
      });
      setPlanningCurrency(saved.planningCurrency);
      setTimeZoneId(saved.timeZoneId);
      router.refresh();
    } catch (error) {
      setProfileError(
        getApiErrorMessage(error, "The household profile could not be saved."),
      );
    } finally {
      setIsSavingProfile(false);
    }
  }

  async function addContributor() {
    const name = newName.trim();
    if (!name) {
      setContributorError("Enter a contributor name.");
      return;
    }

    setIsAdding(true);
    setContributorError(null);

    try {
      const contributor = await addHouseholdContributor({
        name,
        isVisible: newVisible,
      });
      setContributors((current) =>
        [...current, contributor].sort((left, right) =>
          left.name.localeCompare(right.name),
        ),
      );
      setNewName("");
      setNewVisible(true);
    } catch (error) {
      setContributorError(
        getApiErrorMessage(error, "The contributor could not be added."),
      );
    } finally {
      setIsAdding(false);
    }
  }

  async function saveContributor(contributor: HouseholdContributorDto) {
    setContributorError(null);

    try {
      const saved = await updateHouseholdContributor(contributor.id, {
        name: contributor.name,
        isVisible: contributor.isVisible,
      });
      setContributors((current) =>
        current
          .map((item) => (item.id === saved.id ? saved : item))
          .sort((left, right) => left.name.localeCompare(right.name)),
      );
    } catch (error) {
      setContributorError(
        getApiErrorMessage(error, "The contributor could not be saved."),
      );
    }
  }

  async function removeContributor(contributorId: string) {
    setContributorError(null);

    try {
      await removeHouseholdContributor(contributorId);
      setContributors((current) =>
        current.filter((item) => item.id !== contributorId),
      );
    } catch (error) {
      setContributorError(
        getApiErrorMessage(error, "The contributor could not be removed."),
      );
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        eyebrow="Household"
        title="Financial profile"
        description="One planning currency, the household time zone, and the people whose finances belong here. Contributors are names you keep, not separate sign-ins."
      />

      <form
        className="app-panel flex flex-col gap-4 p-4"
        onSubmit={(event) => {
          event.preventDefault();
          void saveProfile();
        }}
      >
        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Planning currency
          <Select
            title="Planning currency"
            value={planningCurrency}
            onChange={setPlanningCurrency}
            options={currencyOptions.map((option) => ({
              value: option.code,
              label: option.label,
            }))}
          />
          <span className="font-normal text-muted-foreground">
            Totals use this currency only. An account or transaction in
            another currency stays listed and is left out until conversion is
            available. A blank currency stays in these totals.
          </span>
        </div>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Time zone
          <Select
            title="Time zone"
            value={timeZoneId}
            onChange={setTimeZoneId}
            options={timeZoneOptions.map((option) => ({
              value: option.id,
              label: option.label,
            }))}
          />
          <span className="font-normal text-muted-foreground">
            Today and the current month follow this time zone.
          </span>
        </div>

        {profileError ? (
          <Alert variant="destructive">{profileError}</Alert>
        ) : null}

        <div>
          <Button type="submit" disabled={isSavingProfile}>
            {isSavingProfile ? "Saving…" : "Save profile"}
          </Button>
        </div>
      </form>

      <section className="app-panel flex flex-col gap-4 p-4">
        <div>
          <h2 className="app-section-title">Contributors</h2>
          <p className="app-section-meta">
            Hide a person to keep the fact without showing them as part of the
            household. Hiding does not change account totals.
          </p>
        </div>

        {contributors.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No contributors yet. The signed-in owner is not added automatically.
          </p>
        ) : (
          <ul className="flex flex-col gap-3">
            {contributors.map((contributor) => (
              <ContributorRow
                key={contributor.id}
                contributor={contributor}
                onChange={(next) =>
                  setContributors((current) =>
                    current.map((item) =>
                      item.id === next.id ? next : item,
                    ),
                  )
                }
                onSave={() => void saveContributor(contributor)}
                onRemove={() => void removeContributor(contributor.id)}
              />
            ))}
          </ul>
        )}

        <form
          className="flex flex-col gap-3 border-t border-border/70 pt-4"
          onSubmit={(event) => {
            event.preventDefault();
            void addContributor();
          }}
        >
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Name
            <Input
              value={newName}
              onChange={(event) => setNewName(event.target.value)}
              maxLength={80}
              autoComplete="off"
            />
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              className="size-4 accent-primary"
              checked={newVisible}
              onChange={(event) => setNewVisible(event.target.checked)}
            />
            Shown in the household
          </label>
          {contributorError ? (
            <Alert variant="destructive">{contributorError}</Alert>
          ) : null}
          <div>
            <Button type="submit" variant="outline" disabled={isAdding}>
              {isAdding ? "Adding…" : "Add contributor"}
            </Button>
          </div>
        </form>
      </section>
    </div>
  );
}

function ContributorRow({
  contributor,
  onChange,
  onSave,
  onRemove,
}: {
  contributor: HouseholdContributorDto;
  onChange: (contributor: HouseholdContributorDto) => void;
  onSave: () => void;
  onRemove: () => void;
}) {
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
      <label className="flex items-center gap-2 pb-2 text-sm">
        <input
          type="checkbox"
          className="size-4 accent-primary"
          checked={contributor.isVisible}
          onChange={(event) =>
            onChange({ ...contributor, isVisible: event.target.checked })
          }
        />
        Shown
      </label>
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

