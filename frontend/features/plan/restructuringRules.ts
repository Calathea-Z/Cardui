import type {
  CompareRestructuringDto,
  DebtKind,
  RestructuringFeeFunding,
  RestructuringOfferKind,
} from "@/lib/api/types";

/**
 * The offer fields before they are sent.
 * Amounts stay text so a blank field can ask to be filled in.
 */
export type RestructuringFormState = {
  kind: RestructuringOfferKind;
  debtIds: string[];
  apr: string;
  termMonths: string;
  fee: string;
  feeFunding: RestructuringFeeFunding;
  upfrontCash: string;
};

/**
 * A debt the form can select. Balance null means the amount owed is unknown.
 */
export type RestructuringFormDebt = {
  debtId: string;
  name: string;
  kind: DebtKind;
  balance: number | null;
  currency: string;
};

export type RestructuringFormResult =
  { ok: true; dto: CompareRestructuringDto } | { ok: false; error: string };

const maxAmount = 100_000_000;

/**
 * The form shown before an offer is compared.
 * Fee and upfront cash start at zero. The rate and term stay blank.
 */
export function initialRestructuringForm(): RestructuringFormState {
  return {
    kind: "ReplacementLoan",
    debtIds: [],
    apr: "",
    termMonths: "",
    fee: "0",
    feeFunding: "Financed",
    upfrontCash: "0",
  };
}

/**
 * Reads the form into the comparison request.
 * The checks match the comparison rule so a field can ask again before the request is sent.
 */
export function readRestructuringForm(
  form: RestructuringFormState,
  debts: RestructuringFormDebt[],
  planningCurrency: string,
): RestructuringFormResult {
  const selected = form.debtIds.map((debtId) =>
    debts.find((debt) => debt.debtId === debtId),
  );
  const kindError = kindErrorFor(form.kind, selected);
  if (kindError) {
    return { ok: false, error: kindError };
  }

  for (const debt of selected) {
    if (!debt) {
      return { ok: false, error: "Choose debts from this household." };
    }

    const debtError = debtErrorFor(debt, planningCurrency);
    if (debtError) {
      return { ok: false, error: debtError };
    }
  }

  const apr = parseApr(form.apr);
  if (!apr.ok) {
    return apr;
  }

  const termMonths = parseTerm(form.termMonths);
  if (!termMonths.ok) {
    return termMonths;
  }

  const fee = parseMoney(
    form.fee,
    "Enter the fee in dollars and cents, or zero.",
  );
  if (!fee.ok) {
    return fee;
  }

  const upfrontCash = parseMoney(
    form.upfrontCash,
    "Enter upfront cash in dollars and cents, or zero.",
  );
  if (!upfrontCash.ok) {
    return upfrontCash;
  }

  return {
    ok: true,
    dto: {
      kind: form.kind,
      debtIds: form.debtIds,
      apr: apr.value,
      termMonths: termMonths.value,
      fee: fee.value,
      feeFunding: form.feeFunding,
      upfrontCash: upfrontCash.value,
    },
  };
}

/**
 * True when two forms would send the same comparison.
 * A difference means the visible result is the previous comparison.
 */
export function sameRestructuringForm(
  left: RestructuringFormState,
  right: RestructuringFormState,
): boolean {
  return (
    left.kind === right.kind &&
    left.apr === right.apr &&
    left.termMonths === right.termMonths &&
    left.fee === right.fee &&
    left.feeFunding === right.feeFunding &&
    left.upfrontCash === right.upfrontCash &&
    left.debtIds.length === right.debtIds.length &&
    left.debtIds.every((debtId, index) => debtId === right.debtIds[index])
  );
}

/**
 * Why a debt cannot be selected. Null when it can.
 */
export function debtSelectionBlock(
  debt: RestructuringFormDebt,
  planningCurrency: string,
): string | null {
  if (!sameCurrency(debt.currency, planningCurrency)) {
    return `${debt.name} is in another currency, so it can't be part of this comparison.`;
  }

  if (debt.balance === null) {
    return `${debt.name} has no balance.`;
  }

  if (debt.balance <= 0) {
    return `${debt.name} is already paid off.`;
  }

  return null;
}

/**
 * Checks the offer kind against the debts already chosen.
 */
function kindErrorFor(
  kind: RestructuringOfferKind,
  selected: (RestructuringFormDebt | undefined)[],
): string | null {
  if (kind === "ReplacementLoan" && selected.length !== 1) {
    return selected.length === 0
      ? "Select the debt this loan replaces."
      : "A replacement loan replaces one debt. Two or more debts are a consolidation loan.";
  }

  if (kind === "ConsolidationLoan" && selected.length < 2) {
    return "A consolidation loan replaces two or more debts. A single debt is a replacement loan.";
  }

  if (kind === "BalanceTransfer") {
    if (selected.length < 1) {
      return "Select at least one revolving debt.";
    }

    const installment = selected.find((debt) => debt?.kind === "Installment");
    if (installment?.name) {
      return `A balance transfer can include only revolving debts. ${installment.name} is an installment loan.`;
    }
  }

  return null;
}

/**
 * Checks one selected debt. A blocked debt uses the same sentence as the list.
 */
function debtErrorFor(
  debt: RestructuringFormDebt,
  planningCurrency: string,
): string | null {
  return debtSelectionBlock(debt, planningCurrency);
}

/**
 * Reads a percent from 0 through 999.999 with up to three decimal places.
 */
function parseApr(
  value: string,
): { ok: true; value: number } | { ok: false; error: string } {
  const text = value.trim();
  if (text === "" || text.startsWith("-")) {
    return { ok: false, error: "Enter the APR as a percent." };
  }

  if (!/^\d+(\.\d{1,3})?$/.test(text)) {
    return {
      ok: false,
      error: "Enter the APR with up to three decimal places.",
    };
  }

  const apr = Number(text);
  if (apr > 999.999) {
    return { ok: false, error: "That APR is too large." };
  }

  return { ok: true, value: apr };
}

/**
 * Reads a whole term from 1 through 600 months.
 */
function parseTerm(
  value: string,
): { ok: true; value: number } | { ok: false; error: string } {
  const text = value.trim();
  if (!/^\d+$/.test(text)) {
    return { ok: false, error: "Enter a term from 1 to 600 months." };
  }

  const months = Number(text);
  if (months < 1 || months > 600) {
    return { ok: false, error: "Enter a term from 1 to 600 months." };
  }

  return { ok: true, value: months };
}

/**
 * Reads a dollar amount. Zero is allowed. More than two decimal places is rejected.
 */
function parseMoney(
  value: string,
  invalid: string,
): { ok: true; value: number } | { ok: false; error: string } {
  const text = value.trim();
  if (text === "" || text.startsWith("-") || !/^\d+(\.\d{1,2})?$/.test(text)) {
    return { ok: false, error: invalid };
  }

  const amount = Number(text);
  if (amount > maxAmount) {
    return { ok: false, error: "That amount is too large." };
  }

  return { ok: true, value: amount };
}

/**
 * True when a blank or matching currency can be planned.
 * Comparison is case-insensitive, matching the planning-currency rule.
 */
function sameCurrency(currency: string, planningCurrency: string): boolean {
  const code = currency.trim();
  if (code === "") {
    return true;
  }

  return code.toLowerCase() === planningCurrency.trim().toLowerCase();
}
