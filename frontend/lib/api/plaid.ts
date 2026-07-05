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