/**
 * One connected institution.
 * Sync timestamps say whether the last pull is running, finished, or failed.
 */
export type PlaidItemDto = {
  id: string;
  institutionId: string | null;
  institutionName: string | null;
  createdAt: string;
  updatedAt: string;
  lastTransactionsSyncedAt: string | null;
  lastSyncStartedAt: string | null;
  lastSyncCompletedAt: string | null;
  lastSyncFailedAt: string | null;
  lastSyncError: string | null;
};

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

export type SyncTransactionsResponseDto = {
  added: number;
  modified: number;
  removed: number;
};

export type SyncPlaidItemResponseDto = {
  plaidItemId: string;
  transactions: SyncTransactionsResponseDto;
};
