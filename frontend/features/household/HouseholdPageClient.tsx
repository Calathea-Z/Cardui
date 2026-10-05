"use client";

import { Alert } from "@/components/ui/alert";
import { Form } from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { PageHeader } from "@/components/navigation/page-header";
import type { FinancialProfileDto } from "@/lib/api/types";
import { ContributorRow } from "./ContributorRow";
import { useHouseholdProfile } from "./useHouseholdProfile";

type HouseholdPageClientProps = {
  profile: FinancialProfileDto;
};

/**
 * Household financial profile.
 * The user sets the planning currency, time zone, and named contributors.
 */
export function HouseholdPageClient({ profile }: HouseholdPageClientProps) {
  const household = useHouseholdProfile(profile);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Financial profile"
        description="One planning currency, the household time zone, and the people whose finances belong here. Contributors are names you keep, not separate sign-ins."
      />

      <Form
        className="app-panel flex flex-col gap-4 p-4"
        onSubmit={(event) => {
          event.preventDefault();
          void household.saveProfile();
        }}
      >
        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Planning currency
          <Select
            title="Planning currency"
            value={household.planningCurrency}
            onChange={household.setPlanningCurrency}
            options={household.currencyOptions.map((option) => ({
              value: option.code,
              label: option.label,
            }))}
          />
          <span className="font-normal text-muted-foreground">
            Totals use this currency only. An account or transaction in another
            currency stays listed and is left out until conversion is available.
            A blank currency stays in these totals.
          </span>
        </div>

        <div className="flex flex-col gap-1.5 text-sm font-medium">
          Time zone
          <Select
            title="Time zone"
            value={household.timeZoneId}
            onChange={household.setTimeZoneId}
            options={household.timeZoneOptions.map((option) => ({
              value: option.id,
              label: option.label,
            }))}
          />
          <span className="font-normal text-muted-foreground">
            Today and the current month follow this time zone.
          </span>
        </div>

        {household.profileError ? (
          <Alert variant="destructive">{household.profileError}</Alert>
        ) : null}

        <div>
          <Button type="submit" disabled={household.isSavingProfile}>
            {household.isSavingProfile ? "Saving…" : "Save profile"}
          </Button>
        </div>
      </Form>

      <section className="app-panel flex flex-col gap-4 p-4">
        <div>
          <h2 className="app-section-title">Contributors</h2>
          <p className="app-section-meta">
            Hide a person to keep the fact without showing them as part of the
            household. Hiding does not change account totals.
          </p>
        </div>

        {household.contributors.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No contributors yet. The signed-in owner is not added automatically.
          </p>
        ) : (
          <ul className="flex flex-col gap-3">
            {household.contributors.map((contributor) => (
              <ContributorRow
                key={contributor.id}
                contributor={contributor}
                onChange={household.replaceContributor}
                onSave={() => void household.saveContributor(contributor)}
                onRemove={() =>
                  void household.removeContributor(contributor.id)
                }
              />
            ))}
          </ul>
        )}

        <Form
          className="flex flex-col gap-3 border-t border-border/70 pt-4"
          onSubmit={(event) => {
            event.preventDefault();
            void household.addContributor();
          }}
        >
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Name
            <Input
              value={household.newName}
              onChange={(event) => household.setNewName(event.target.value)}
              maxLength={80}
              autoComplete="off"
            />
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              className="size-4 accent-primary"
              checked={household.newVisible}
              onChange={(event) =>
                household.setNewVisible(event.target.checked)
              }
            />
            Shown in the household
          </label>
          {household.contributorError ? (
            <Alert variant="destructive">{household.contributorError}</Alert>
          ) : null}
          <div>
            <Button
              type="submit"
              variant="outline"
              disabled={household.isAdding}
            >
              {household.isAdding ? "Adding…" : "Add contributor"}
            </Button>
          </div>
        </Form>
      </section>
    </div>
  );
}
