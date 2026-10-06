import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getAccounts } from "@/lib/api/server/accounts";
import { getDebtSummary, getDebts } from "@/lib/api/server/debts";
import type { DebtSummaryReportDto } from "@/lib/api/types";
import { getFinancialProfile } from "@/lib/api/server/households";
import type { PageLoadState } from "@/lib/pageLoadState";
import { emptyFinancialProfile } from "@/features/household/server/loadHouseholdPage";
import type { DebtsPageData } from "../debtPageData";

/**
 * Loads debts, their summary, open accounts, and the planning currency.
 * A failed request still returns a page, with the error for the banner.
 * Summary stays empty when the debt list itself failed, so totals are not shown without the debts.
 */
export async function loadDebtsPage(): Promise<PageLoadState<DebtsPageData>> {
  const [debts, summary, accounts, profile] = await Promise.all([
    safeApiCall(getDebts, []),
    safeApiCall<DebtSummaryReportDto | null>(getDebtSummary, null),
    safeApiCall(getAccounts, []),
    safeApiCall(getFinancialProfile, emptyFinancialProfile()),
  ]);

  return {
    data: {
      debts: debts.data,
      summary: debts.error ? null : summary.data,
      accounts: accounts.data,
      planningCurrency: profile.data.planningCurrency,
    },
    error: firstApiError(debts, summary, accounts, profile),
  };
}
