import { InstitutionsPageClient } from "@/features/institutions/InstitutionsPageClient";
import {
  emptyAccounts,
  emptyPlaidItems,
  getAccounts,
  getPlaidItems,
  safeApiCall,
} from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function InstitutionsPage() {
  const [plaidItemsResult, accountsResult] = await Promise.all([
    safeApiCall(getPlaidItems, emptyPlaidItems()),
    safeApiCall(getAccounts, emptyAccounts()),
  ]);

  return (
    <InstitutionsPageClient
      initialItems={plaidItemsResult.data}
      accounts={accountsResult.data}
    />
  );
}
