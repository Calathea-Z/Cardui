import { InstitutionsPageClient } from "@/features/institutions/InstitutionsPageClient";
import { getAccounts, getPlaidItems } from "@/lib/api";

export const revalidate = 30;

export default async function InstitutionsPage() {
  const [plaidItems, accounts] = await Promise.all([
    getPlaidItems(),
    getAccounts(),
  ]);

  return (
    <InstitutionsPageClient initialItems={plaidItems} accounts={accounts} />
  );
}
