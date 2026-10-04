"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
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

/**
 * Sorts contributors by name without changing the input list.
 */
function sortContributors(contributors: HouseholdContributorDto[]) {
  return contributors
    .slice()
    .sort((left, right) => left.name.localeCompare(right.name));
}

/**
 * Saves the household profile and adds, edits, or removes contributors.
 * A saved profile refreshes the page. Contributor edits stay on this list.
 */
export function useHouseholdProfile(profile: FinancialProfileDto) {
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

  /**
   * Saves the planning currency and time zone, then refreshes the page.
   */
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

  /**
   * Adds a contributor by name.
   * A blank name is rejected before the request, and the new person is sorted into the list.
   */
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
      setContributors((current) => sortContributors([...current, contributor]));
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

  /**
   * Replaces one contributor in the list while the row is being edited.
   * The change is kept locally until that row is saved.
   */
  function replaceContributor(contributor: HouseholdContributorDto) {
    setContributors((current) =>
      current.map((item) => (item.id === contributor.id ? contributor : item)),
    );
  }

  /**
   * Saves one contributor's name and visibility, then sorts the list by name.
   */
  async function saveContributor(contributor: HouseholdContributorDto) {
    setContributorError(null);

    try {
      const saved = await updateHouseholdContributor(contributor.id, {
        name: contributor.name,
        isVisible: contributor.isVisible,
      });
      setContributors((current) =>
        sortContributors(
          current.map((item) => (item.id === saved.id ? saved : item)),
        ),
      );
    } catch (error) {
      setContributorError(
        getApiErrorMessage(error, "The contributor could not be saved."),
      );
    }
  }

  /**
   * Removes a contributor from the household.
   * A failed request shows an error on the contributor section.
   */
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

  return {
    planningCurrency,
    setPlanningCurrency,
    timeZoneId,
    setTimeZoneId,
    contributors,
    currencyOptions,
    timeZoneOptions,
    profileError,
    contributorError,
    isSavingProfile,
    newName,
    setNewName,
    newVisible,
    setNewVisible,
    isAdding,
    saveProfile,
    addContributor,
    replaceContributor,
    saveContributor,
    removeContributor,
  };
}
