import { apiClient } from "./client";
import type {
    PagedResultDto,
    TransactionDto,
    TransactionQueryDto,
    UpdateTransactionCategoryDto,
} from "./types";

export async function getTransactions(
    query?: TransactionQueryDto,
): Promise<PagedResultDto<TransactionDto>> {
    const response = await apiClient.get<PagedResultDto<TransactionDto>>(
        "/api/transactions",
        {
            params: query,
        },
    );

    return response.data;
}

export async function getTransactionById(id: string): Promise<TransactionDto> {
    const response = await apiClient.get<TransactionDto>(
        `/api/transactions/${id}`,
    );

    return response.data;
}

export async function updateTransactionCategory(
    id: string,
    dto: UpdateTransactionCategoryDto,
): Promise<TransactionDto> {
    const response = await apiClient.patch<TransactionDto>(
        `/api/transactions/${id}/category`,
        dto,
    );

    return response.data;
}
