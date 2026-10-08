import type { SavingsAccountDto, SavingsGoalDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/savings-goals
 * Loads the household's operating reserve, emergency goal, and named savings goals.
 */
export async function getSavingsGoals(): Promise<SavingsGoalDto[]> {
  const response =
    await serverClient.get<SavingsGoalDto[]>("/api/savings-goals");
  return response.data;
}

/**
 * GET /api/savings-goals/accounts
 * Loads cash accounts a goal may follow.
 */
export async function getSavingsAccounts(): Promise<SavingsAccountDto[]> {
  const response = await serverClient.get<SavingsAccountDto[]>(
    "/api/savings-goals/accounts",
  );
  return response.data;
}
