import { apiClient } from "./client";

export type CreatePlaidLinkTokenResponse = {
    linkToken: string;
};

export type ExchangePlaidPublicTokenRequest = {
    publicToken: string;
    institutionId?: string;
    institutionName?: string;
};

export type ExchangePlaidPublicTokenResponse = {
    plaidItemId: string;
};

export type PlaidItemDto = {
    id: string;
    institutionId: string | null;
    institutionName: string | null;
    createdAt: string;
    updatedAt: string;
    lastTransactionsSyncedAt: string | null;
};

export type SyncTransactionsResponseDto = {
    added: number;
    modified: number;
    removed: number;
    nextCursor: string | null;
};

export type SyncPlaidItemResponseDto = {
    plaidItemId: string;
    transactions: SyncTransactionsResponseDto;
};

export async function getPlaidItems(): Promise<PlaidItemDto[]> {
    const response = await apiClient.get<PlaidItemDto[]>("/api/plaid/items");

    return response.data;
}

export async function createPlaidLinkToken(): Promise<CreatePlaidLinkTokenResponse> {
    const response = await apiClient.post<CreatePlaidLinkTokenResponse>(
        "/api/plaid/link-token",
    );

    return response.data;
}

export async function exchangePlaidPublicToken(
    dto: ExchangePlaidPublicTokenRequest,
): Promise<ExchangePlaidPublicTokenResponse> {
    const response = await apiClient.post<ExchangePlaidPublicTokenResponse>(
        "/api/plaid/exchange-public-token",
        dto,
    );

    return response.data;
}

export async function syncPlaidItem(
    plaidItemId: string,
): Promise<SyncPlaidItemResponseDto> {
    const response = await apiClient.post<SyncPlaidItemResponseDto>(
        `/api/plaid/${plaidItemId}/sync`,
    );

    return response.data;
}