import type { AccountDto, ObligationDto } from "@/lib/api/types";

/**
 * Data the bills page renders.
 * `planningCurrency` is the household currency used for a new bill.
 * Each bill still displays the currency stored when it was saved.
 * `accounts` are the open household accounts that can pay a bill.
 */
export type BillsPageData = {
  obligations: ObligationDto[];
  accounts: AccountDto[];
  planningCurrency: string;
};
