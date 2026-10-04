"use client";

import { useEffect, useRef, useState } from "react";
import {
  archiveTransaction,
  restoreTransaction,
  updateTransactionDetails,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { CategoryDto, TransactionDto } from "@/lib/api/types";
import {
  toFormState,
  toOptimisticTransaction,
  toUpdateDto,
  type AmountDirection,
  type TransactionDetailsFormState,
} from "./transactionEntry";

type UseTransactionDetailsOptions = {
  transaction: TransactionDto;
  categories: CategoryDto[];
  onSaved: (transaction: TransactionDto) => void;
  onArchived: (transactionId: string) => void;
  onRestored: () => void;
};

/**
 * How long a notes, name, or amount edit waits before it is saved.
 */
const TEXT_SAVE_DELAY_MS = 400;

/**
 * Keeps the transaction detail form and saves each change.
 * A response from an older edit is ignored, and a failed save restores the last saved transaction.
 */
export function useTransactionDetails({
  transaction,
  categories,
  onSaved,
  onArchived,
  onRestored,
}: UseTransactionDetailsOptions) {
  const [form, setForm] = useState<TransactionDetailsFormState>(() =>
    toFormState(transaction),
  );
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [archiveError, setArchiveError] = useState<string | null>(null);
  const [isArchiving, setIsArchiving] = useState(false);
  const [confirmArchive, setConfirmArchive] = useState(false);

  const formRef = useRef(form);
  const committedRef = useRef(transaction);
  const editVersionRef = useRef(0);
  const textTimeoutRef = useRef<number | null>(null);
  const categoriesRef = useRef(categories);
  const onSavedRef = useRef(onSaved);
  const onArchivedRef = useRef(onArchived);
  const onRestoredRef = useRef(onRestored);
  const flushRef = useRef<() => void>(() => {});

  useEffect(() => {
    categoriesRef.current = categories;
  }, [categories]);

  useEffect(() => {
    onSavedRef.current = onSaved;
  }, [onSaved]);

  useEffect(() => {
    onArchivedRef.current = onArchived;
  }, [onArchived]);

  useEffect(() => {
    onRestoredRef.current = onRestored;
  }, [onRestored]);

  useEffect(() => {
    return () => {
      flushRef.current();
    };
  }, []);

  /**
   * Sends the form to the server.
   * A newer edit discards this result, and a failure restores the form from the last saved transaction.
   */
  async function persist(
    nextForm: TransactionDetailsFormState,
    versionAtStart: number,
  ) {
    const previous = committedRef.current;

    try {
      const updated = await updateTransactionDetails(
        previous.id,
        toUpdateDto(nextForm, previous),
      );

      committedRef.current = updated;

      if (versionAtStart !== editVersionRef.current) {
        return;
      }

      setErrorMessage(null);
      onSavedRef.current(updated);
    } catch (error) {
      if (versionAtStart !== editVersionRef.current) {
        return;
      }

      const reverted = toFormState(committedRef.current);
      setForm(reverted);
      formRef.current = reverted;
      onSavedRef.current(committedRef.current);
      setErrorMessage(
        getApiErrorMessage(error, "Could not save transaction details."),
      );
    }
  }

  /**
   * Clears the timer for a text edit that is still waiting to save.
   */
  function clearTextTimeout() {
    if (textTimeoutRef.current !== null) {
      window.clearTimeout(textTimeoutRef.current);
      textTimeoutRef.current = null;
    }
  }

  /**
   * Shows the next form on the detail and in the list before the save returns.
   * Each call advances the edit version used to ignore an older response.
   */
  function applyOptimistic(nextForm: TransactionDetailsFormState) {
    editVersionRef.current += 1;
    setForm(nextForm);
    formRef.current = nextForm;
    setErrorMessage(null);
    onSavedRef.current(
      toOptimisticTransaction(
        committedRef.current,
        nextForm,
        categoriesRef.current,
      ),
    );
    return editVersionRef.current;
  }

  /**
   * Saves a date, category, or direction change immediately.
   * A waiting text save is cancelled first.
   */
  function persistNow(nextForm: TransactionDetailsFormState) {
    clearTextTimeout();
    const version = applyOptimistic(nextForm);
    void persist(nextForm, version);
  }

  /**
   * Shows a text edit immediately and saves it after the text delay.
   * Another keystroke replaces the waiting save.
   */
  function persistText(nextForm: TransactionDetailsFormState) {
    const version = applyOptimistic(nextForm);
    clearTextTimeout();
    textTimeoutRef.current = window.setTimeout(() => {
      textTimeoutRef.current = null;
      void persist(formRef.current, version);
    }, TEXT_SAVE_DELAY_MS);
  }

  function setCategoryId(categoryId: string) {
    persistNow({ ...formRef.current, categoryId });
  }

  function setDate(date: string) {
    persistNow({ ...formRef.current, date });
  }

  function setNotes(notes: string) {
    persistText({ ...formRef.current, notes });
  }

  function setName(name: string) {
    persistText({ ...formRef.current, name });
  }

  function setAmount(amount: string) {
    persistText({ ...formRef.current, amount });
  }

  function setDirection(direction: AmountDirection) {
    persistNow({ ...formRef.current, direction });
  }

  /**
   * Remembers a category created while the detail is open.
   * A category already in the list is left unchanged.
   */
  function registerCategory(category: CategoryDto) {
    if (!categoriesRef.current.some((item) => item.id === category.id)) {
      categoriesRef.current = [...categoriesRef.current, category];
    }
  }

  /**
   * Sends a text edit that is still waiting, such as when the detail closes.
   * Nothing is sent when no save is waiting.
   */
  function flushPendingSave() {
    if (textTimeoutRef.current === null) {
      return;
    }

    clearTextTimeout();
    void persist(formRef.current, editVersionRef.current);
  }

  useEffect(() => {
    flushRef.current = flushPendingSave;
  });

  /**
   * Archives a live transaction or restores one that is already archived.
   * A failure stays on the detail, and the archive confirmation closes either way.
   */
  async function archiveOrRestore() {
    setIsArchiving(true);
    setArchiveError(null);
    try {
      if (transaction.archivedAt) {
        await restoreTransaction(transaction.id);
        onRestoredRef.current();
        return;
      }

      await archiveTransaction(transaction.id);
      onArchivedRef.current(transaction.id);
    } catch (error) {
      setArchiveError(
        getApiErrorMessage(error, "Could not update this transaction."),
      );
    } finally {
      setIsArchiving(false);
      setConfirmArchive(false);
    }
  }

  /**
   * Restores an archived transaction immediately, or asks before archiving a live one.
   */
  function beginArchive() {
    if (transaction.archivedAt) {
      void archiveOrRestore();
      return;
    }

    setConfirmArchive(true);
  }

  return {
    form,
    errorMessage,
    clearError: () => setErrorMessage(null),
    setDate,
    setCategoryId,
    setNotes,
    setName,
    setAmount,
    setDirection,
    registerCategory,
    archiveError,
    isArchiving,
    confirmArchive,
    beginArchive,
    cancelArchive: () => setConfirmArchive(false),
    archiveOrRestore,
  };
}
