import { apiClient } from "./client";
import type { AccountDto } from "./types";

export async function getAccounts(): Promise<AccountDto[]> {
    const response = await apiClient.get<AccountDto[]>("/api/accounts");

    return response.data;
}