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

const TEXT_SAVE_DELAY_MS = 400;

export function canEditTransactionEntry(transaction: TransactionDto) {
  return (
    (transaction.source === "Manual" &&
      transaction.provenance === "ManualEntry") ||
    (transaction.source === "Csv" && transaction.provenance === "CsvImport")
  );
}

function directionFor(amount: number): AmountDirection {
  return amount < 0 ? "in" : "out";
}

function toSignedAmount(amount: string, direction: AmountDirection) {
  const parsed = Number(amount);
  if (!Number.isFinite(parsed)) {
    return null;
  }

  const absolute = Math.abs(parsed);
  return direction === "in" ? -absolute : absolute;
}

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

  function clearTextTimeout() {
    if (textTimeoutRef.current !== null) {
      window.clearTimeout(textTimeoutRef.current);
      textTimeoutRef.current = null;
    }
  }

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

  function persistNow(nextForm: TransactionDetailsFormState) {
    clearTextTimeout();
    const version = applyOptimistic(nextForm);
    void persist(nextForm, version);
  }

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

  function registerCategory(category: CategoryDto) {
    if (!categoriesRef.current.some((item) => item.id === category.id)) {
      categoriesRef.current = [...categoriesRef.current, category];
    }
  }

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
