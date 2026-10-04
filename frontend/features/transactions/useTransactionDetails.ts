"use client";

import { useEffect, useRef, useState } from "react";
import { updateTransactionDetails } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  CategoryDto,
  TransactionCategoryDto,
  TransactionDto,
  UpdateTransactionDetailsDto,
} from "@/lib/api/types";

type AmountDirection = "out" | "in";

type TransactionDetailsFormState = {
  date: string;
  categoryId: string;
  notes: string;
  name: string;
  amount: string;
  direction: AmountDirection;
};

type UseTransactionDetailsOptions = {
  transaction: TransactionDto;
  categories: CategoryDto[];
  onSaved: (transaction: TransactionDto) => void;
};

/**
 * How long a notes, name, or amount edit waits before it is saved.
 */
const TEXT_SAVE_DELAY_MS = 400;

/**
 * True when the household can change the name and amount.
 * A manual entry with manual provenance, or a CSV import, can be edited.
 */
export function canEditTransactionEntry(transaction: TransactionDto) {
  return (
    (transaction.source === "Manual" &&
      transaction.provenance === "ManualEntry") ||
    (transaction.source === "Csv" && transaction.provenance === "CsvImport")
  );
}

/**
 * Reads a stored amount as money in or money out.
 * A negative amount is money in, and zero or a positive amount is money out.
 */
function directionFor(amount: number): AmountDirection {
  return amount < 0 ? "in" : "out";
}

/**
 * Turns the typed amount and direction into the number that will be stored.
 * Money in is negative, money out is positive, and an unreadable amount returns null.
 */
function toSignedAmount(amount: string, direction: AmountDirection) {
  const parsed = Number(amount);
  if (!Number.isFinite(parsed)) {
    return null;
  }

  const absolute = Math.abs(parsed);
  return direction === "in" ? -absolute : absolute;
}

/**
 * Copies a transaction into the detail form.
 * The amount is the absolute value, and a missing category is an empty id.
 */
function toFormState(transaction: TransactionDto): TransactionDetailsFormState {
  return {
    date: transaction.date,
    categoryId: transaction.category?.id ?? "",
    notes: transaction.notes ?? "",
    name: transaction.name,
    amount: String(Math.abs(transaction.amount)),
    direction: directionFor(transaction.amount),
  };
}

/**
 * Builds the category shown on an unsaved transaction.
 * An empty id or an id missing from the list becomes null.
 */
function toCategoryDto(
  categories: CategoryDto[],
  categoryId: string,
): TransactionCategoryDto | null {
  if (!categoryId) {
    return null;
  }

  const category = categories.find((item) => item.id === categoryId);
  if (!category) {
    return null;
  }

  return {
    id: category.id,
    name: category.name,
    key: category.key,
    color: category.color,
    icon: category.icon,
  };
}

/**
 * Builds the transaction the list shows before the save returns.
 * Name and amount change for an editable entry, and a blank name keeps the current name.
 */
function toOptimisticTransaction(
  transaction: TransactionDto,
  form: TransactionDetailsFormState,
  categories: CategoryDto[],
): TransactionDto {
  const next: TransactionDto = {
    ...transaction,
    date: form.date,
    category: toCategoryDto(categories, form.categoryId),
    notes: form.notes.trim() || null,
  };

  if (!canEditTransactionEntry(transaction)) {
    return next;
  }

  const name = form.name.trim();
  const amount = toSignedAmount(form.amount, form.direction);
  return {
    ...next,
    name: name || transaction.name,
    merchantName: name || transaction.merchantName,
    amount: amount ?? transaction.amount,
  };
}

/**
 * Builds the payload sent when the detail form is saved.
 * Name and amount are included for an editable entry, and a blank name or unreadable amount stops the save.
 */
function toUpdateDto(
  form: TransactionDetailsFormState,
  transaction: TransactionDto,
): UpdateTransactionDetailsDto {
  const dto: UpdateTransactionDetailsDto = {
    date: form.date,
    categoryId: form.categoryId || null,
    notes: form.notes.trim() || null,
  };

  if (!canEditTransactionEntry(transaction)) {
    return dto;
  }

  const amount = toSignedAmount(form.amount, form.direction);
  if (amount === null) {
    throw new Error("Enter an amount.");
  }

  const name = form.name.trim();
  if (!name) {
    throw new Error("Enter a name.");
  }

  dto.name = name;
  dto.amount = amount;
  return dto;
}

/**
 * Keeps the transaction detail form and saves each change.
 * A response from an older edit is ignored, and a failed save restores the last saved transaction.
 */
export function useTransactionDetails({
  transaction,
  categories,
  onSaved,
}: UseTransactionDetailsOptions) {
  const [form, setForm] = useState<TransactionDetailsFormState>(() =>
    toFormState(transaction),
  );
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const formRef = useRef(form);
  const committedRef = useRef(transaction);
  const editVersionRef = useRef(0);
  const textTimeoutRef = useRef<number | null>(null);
  const categoriesRef = useRef(categories);
  const onSavedRef = useRef(onSaved);

  useEffect(() => {
    categoriesRef.current = categories;
  }, [categories]);

  useEffect(() => {
    onSavedRef.current = onSaved;
  }, [onSaved]);

  useEffect(() => {
    return () => {
      if (textTimeoutRef.current !== null) {
        window.clearTimeout(textTimeoutRef.current);
      }
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
    flushPendingSave,
  };
}
