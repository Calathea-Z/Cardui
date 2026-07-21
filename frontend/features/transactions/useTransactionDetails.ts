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

type TransactionDetailsFormState = {
  date: string;
  categoryId: string;
  notes: string;
};

type UseTransactionDetailsOptions = {
  transaction: TransactionDto;
  categories: CategoryDto[];
  onSaved: (transaction: TransactionDto) => void;
};

const NOTES_SAVE_DELAY_MS = 400;

function toFormState(transaction: TransactionDto): TransactionDetailsFormState {
  return {
    date: transaction.date,
    categoryId: transaction.category?.id ?? "",
    notes: transaction.notes ?? "",
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
  return {
    ...transaction,
    date: form.date,
    category: toCategoryDto(categories, form.categoryId),
    notes: form.notes.trim() || null,
  };
}

function toUpdateDto(
  form: TransactionDetailsFormState,
): UpdateTransactionDetailsDto {
  return {
    date: form.date,
    categoryId: form.categoryId || null,
    notes: form.notes.trim() || null,
  };
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
  const notesTimeoutRef = useRef<number | null>(null);
  const categoriesRef = useRef(categories);
  const onSavedRef = useRef(onSaved);

  formRef.current = form;
  onSavedRef.current = onSaved;

  useEffect(() => {
    categoriesRef.current = categories;
  }, [categories]);

  useEffect(() => {
    return () => {
      if (notesTimeoutRef.current !== null) {
        window.clearTimeout(notesTimeoutRef.current);
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
        toUpdateDto(nextForm),
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

  function clearNotesTimeout() {
    if (notesTimeoutRef.current !== null) {
      window.clearTimeout(notesTimeoutRef.current);
      notesTimeoutRef.current = null;
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

  function setCategoryId(categoryId: string) {
    clearNotesTimeout();
    const nextForm = { ...formRef.current, categoryId };
    const version = applyOptimistic(nextForm);
    void persist(nextForm, version);
  }

  function setDate(date: string) {
    clearNotesTimeout();
    const nextForm = { ...formRef.current, date };
    const version = applyOptimistic(nextForm);
    void persist(nextForm, version);
  }

  function setNotes(notes: string) {
    const nextForm = { ...formRef.current, notes };
    const version = applyOptimistic(nextForm);

    clearNotesTimeout();
    notesTimeoutRef.current = window.setTimeout(() => {
      notesTimeoutRef.current = null;
      void persist(formRef.current, version);
    }, NOTES_SAVE_DELAY_MS);
  }

  function registerCategory(category: CategoryDto) {
    if (!categoriesRef.current.some((item) => item.id === category.id)) {
      categoriesRef.current = [...categoriesRef.current, category];
    }
  }

  function flushPendingSave() {
    if (notesTimeoutRef.current === null) {
      return;
    }

    clearNotesTimeout();
    void persist(formRef.current, editVersionRef.current);
  }

  return {
    form,
    errorMessage,
    clearError: () => setErrorMessage(null),
    setDate,
    setCategoryId,
    setNotes,
    registerCategory,
    flushPendingSave,
  };
}
