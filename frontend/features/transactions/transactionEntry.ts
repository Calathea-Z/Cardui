import { moneyCommaError } from "@/features/accounts/formatCurrency";
import { parseMoney } from "@/features/accounts/manualAccount";
import type {
  CategoryDto,
  TransactionCategoryDto,
  TransactionDto,
  UpdateTransactionDetailsDto,
} from "@/lib/api/types";

/** Money out is stored as a positive amount. Money in is stored as a negative amount. */
export type AmountDirection = "out" | "in";

/**
 * Values the transaction detail form edits.
 * The amount is unsigned. `direction` supplies the sign that will be stored.
 */
export type TransactionDetailsFormState = {
  date: string;
  categoryId: string;
  notes: string;
  name: string;
  amount: string;
  direction: AmountDirection;
};

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
export function directionFor(amount: number): AmountDirection {
  return amount < 0 ? "in" : "out";
}

/**
 * Turns the typed amount and direction into the number that will be stored.
 * Money in is negative, money out is positive, and an unreadable amount returns null.
 */
export function toSignedAmount(amount: string, direction: AmountDirection) {
  const parsed = parseMoney(amount);
  if (parsed === null) {
    return null;
  }

  const absolute = Math.abs(parsed);
  return direction === "in" ? -absolute : absolute;
}

/**
 * Copies a transaction into the detail form.
 * The amount is the absolute value, and a missing category is an empty id.
 */
export function toFormState(
  transaction: TransactionDto,
): TransactionDetailsFormState {
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
export function toOptimisticTransaction(
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
export function toUpdateDto(
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
    throw new Error(moneyCommaError(form.amount) ?? "Enter an amount.");
  }

  const name = form.name.trim();
  if (!name) {
    throw new Error("Enter a name.");
  }

  dto.name = name;
  dto.amount = amount;
  return dto;
}
