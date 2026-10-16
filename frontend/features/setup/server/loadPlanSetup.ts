import { loadAllocationPage } from "@/features/allocation/server/loadAllocationPage";
import { loadBillsPage } from "@/features/bills/server/loadBillsPage";
import { loadDebtsPage } from "@/features/debts/server/loadDebtsPage";
import { loadIncomePage } from "@/features/income/server/loadIncomePage";
import { loadLivingPage } from "@/features/living/server/loadLivingPage";
import { finishPlanItems } from "@/features/plan/planCopy";
import { recoveryPath } from "@/features/plan/planPayoffChoice";
import { loadPlanPage } from "@/features/plan/server/loadPlanPage";
import { loadSavingsPage } from "@/features/savings/server/loadSavingsPage";
import { firstApiError } from "@/lib/api/server";
import type { PageLoadState } from "@/lib/pageLoadState";
import {
  planSetupDebtTermsIssue,
  type PlanSetupInput,
} from "../planSetupSummary";

/**
 * Loads the existing editors' saved facts for Plan setup.
 * A failed request still returns a page. That group's summary says it could not be read.
 * The debt-terms warning is the same debt list Plan shows. A failed plan load leaves that warning off.
 */
export async function loadPlanSetup(): Promise<PageLoadState<PlanSetupInput>> {
  const [income, bills, debts, living, savings, allocation, plan] =
    await Promise.all([
      loadIncomePage(),
      loadBillsPage(),
      loadDebtsPage(),
      loadLivingPage(),
      loadSavingsPage(),
      loadAllocationPage(),
      loadPlanPage(),
    ]);
  const floor = savings.data.goals.find((goal) => goal.kind === "Floor");
  const emergency = savings.data.goals.find(
    (goal) => goal.kind === "Emergency",
  );

  return {
    data: {
      currency: income.data.planningCurrency,
      incomeAvailable: income.error === null,
      sourceCount: income.data.sources.length,
      sharesAvailable: living.error === null,
      contributions: living.error
        ? []
        : living.data.contributions.map((contribution) => ({
            monthlyAmount: contribution.monthlyAmount,
          })),
      billsAvailable: bills.error === null,
      billCount: bills.data.obligations.length,
      debtsAvailable: debts.error === null,
      debtCount: debts.data.debts.length,
      livingAvailable: living.error === null,
      livingMonthly: living.data.livingSpending?.monthlyAmount ?? null,
      savingsAvailable: savings.error === null,
      floorAmount: floor?.floorAmount ?? null,
      emergencyName: emergency?.name ?? null,
      emergencyTarget: emergency?.targetAmount ?? null,
      namedGoalCount: savings.data.goals.filter(
        (goal) => goal.kind === "Sinking",
      ).length,
      allocationAvailable: allocation.error === null,
      allocationSaved: allocation.data.isSaved,
      variableIncome: allocation.data.variableIncome,
      windfall: allocation.data.windfall,
      debtTermsIssue: plan.error
        ? null
        : planSetupDebtTermsIssue(
            finishPlanItems(
              plan.data,
              recoveryPath(plan.data, "Rollover"),
              () => "",
            ).filter((item) => item.isDebt).length,
          ),
    },
    error: firstApiError(income, bills, debts, living, savings, allocation),
  };
}
