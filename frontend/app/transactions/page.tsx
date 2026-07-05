import { getCategories } from "@/lib/api/categories";
import { getTransactions } from "@/lib/api/transactions";
import { TransactionsClient } from "../../features/transactions/transactions-client";

export default async function TransactionsPage() {
    const [transactions, categories] = await Promise.all([
        getTransactions(),
        getCategories(),
    ]);

    return (
        <TransactionsClient
            initialTransactions={transactions}
            categories={categories}
        />
    );
}
