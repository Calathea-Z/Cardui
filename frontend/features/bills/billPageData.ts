import type {
  AccountDto,
  ObligationDto,
  ObligationSuggestionDto,
} from "@/lib/api/types";

/**
 * Data the bills page renders.
 * `planningCurrency` is the household currency used for a new bill.
 * Each bill still displays the currency stored when it was saved.
 * `accounts` are the open household accounts that can pay a bill.
 * `suggestions` are recurring payments noticed in activity. They are not bills.
 */
export type BillsPageData = {
  obligations: ObligationDto[];
  suggestions: ObligationSuggestionDto[];
  accounts: AccountDto[];
  planningCurrency: string;
};
