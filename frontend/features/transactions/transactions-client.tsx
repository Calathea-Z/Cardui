"use client";

import { useMemo, useState } from "react";
import {
    getTransactions,
    updateTransactionCategory,
} from "@/lib/api/transactions";
import { CategoryDto, TransactionDto } from "@/lib/api/types";

type TransactionsClientProps = {
    initialTransactions: TransactionDto[];
    categories: CategoryDto[];
};

function formatCurrency(value: number) {
    const isIncome = value < 0;

    return new Intl.NumberFormat("en-US", {
        style: "currency",
        currency: "USD",
    }).format(isIncome ? Math.abs(value) : value);
}

function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-US", {
        month: "short",
        day: "numeric",
        year: "numeric",
    }).format(new Date(value));
}

export function TransactionsClient({initialTransactions, categories,}: TransactionsClientProps) {
    const [transactions, setTransactions] = useState(initialTransactions);
    const [search, setSearch] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    const categoryOptions = useMemo(
        () => categories.slice().sort((a, b) => a.name.localeCompare(b.name)),
        [categories],
    );

    async function handleSearchSubmit(event: React.FormEvent<HTMLFormElement>) {
        event.preventDefault();

        setIsLoading(true);

        try {
            const updatedTransactions = await getTransactions({
                search: search.trim() || undefined,
            });

            setTransactions(updatedTransactions);
        } finally {
            setIsLoading(false);
        }
    }

    async function handleCategoryChange(transactionId: string, categoryId: string) {
        const nextCategoryId = categoryId === "uncategorized" ? null : categoryId;

        const previousTransactions = transactions;

        setTransactions((currentTransactions) =>
            currentTransactions.map((transaction) => {
                if (transaction.id !== transactionId) {
                    return transaction;
                }

                const nextCategory =
                    nextCategoryId === null
                        ? null
                        : categories.find((category) => category.id === nextCategoryId) ??
                        null;

                return {
                    ...transaction,
                    category: nextCategory
                        ? {
                            id: nextCategory.id,
                            name: nextCategory.name,
                            color: nextCategory.color,
                            icon: nextCategory.icon,
                        }
                        : null,
                };
            }),
        );

        try {
            const updatedTransaction = await updateTransactionCategory(transactionId, {
                categoryId: nextCategoryId,
            });

            setTransactions((currentTransactions) =>
                currentTransactions.map((transaction) =>
                    transaction.id === transactionId ? updatedTransaction : transaction,
                ),
            );
        } catch {
            setTransactions(previousTransactions);
        }
    }

    return (
        <main className="min-h-screen bg-slate-950 text-white">
            <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
                <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
                    <div>
                        <p className="text-sm text-slate-400">Money movement</p>
                        <h1 className="text-3xl font-semibold">Transactions</h1>
                    </div>

                    <form
                        onSubmit={handleSearchSubmit}
                        className="flex w-full gap-2 md:w-auto"
                    >
                        <input
                            value={search}
                            onChange={(event) => setSearch(event.target.value)}
                            placeholder="Search transactions"
                            className="h-10 w-full rounded-md border border-slate-700 bg-slate-900 px-3 text-sm outline-none transition focus:border-emerald-400 md:w-72"
                        />

                        <button
                            type="submit"
                            disabled={isLoading}
                            className="h-10 rounded-md bg-emerald-500 px-4 text-sm font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                            {isLoading ? "Searching" : "Search"}
                        </button>
                    </form>
                </div>

                <div className="overflow-hidden rounded-lg border border-slate-800 bg-slate-900">
                    <div className="grid grid-cols-[1fr_140px_180px] gap-4 border-b border-slate-800 px-4 py-3 text-sm font-medium text-slate-400 md:grid-cols-[140px_1fr_160px_180px_130px]">
                        <span className="hidden md:block">Date</span>
                        <span>Description</span>
                        <span className="hidden md:block">Account</span>
                        <span>Category</span>
                        <span className="text-right">Amount</span>
                    </div>

                    <div className="divide-y divide-slate-800">
                        {transactions.map((transaction) => (
                            <div
                                key={transaction.id}
                                className="grid grid-cols-[1fr_140px_180px] gap-4 px-4 py-4 text-sm md:grid-cols-[140px_1fr_160px_180px_130px]"
                            >
                                <div className="hidden text-slate-400 md:block">
                                    {formatDate(transaction.date)}
                                </div>

                                <div className="min-w-0">
                                    <div className="flex items-center gap-2">
                                        <p className="truncate font-medium">{transaction.name}</p>
                                        {transaction.pending ? (
                                            <span className="rounded-full bg-amber-400/10 px-2 py-0.5 text-xs text-amber-300">
                        Pending
                      </span>
                                        ) : null}
                                    </div>

                                    <p className="mt-1 truncate text-slate-400 md:hidden">
                                        {formatDate(transaction.date)} · {transaction.account.name}
                                    </p>

                                    {transaction.merchantName ? (
                                        <p className="mt-1 truncate text-slate-500">
                                            {transaction.merchantName}
                                        </p>
                                    ) : null}
                                </div>

                                <div className="hidden truncate text-slate-400 md:block">
                                    {transaction.account.name}
                                </div>

                                <select
                                    value={transaction.category?.id ?? "uncategorized"}
                                    onChange={(event) =>
                                        handleCategoryChange(transaction.id, event.target.value)
                                    }
                                    className="h-9 rounded-md border border-slate-700 bg-slate-950 px-2 text-sm outline-none transition focus:border-emerald-400"
                                >
                                    <option value="uncategorized">Uncategorized</option>
                                    {categoryOptions.map((category) => (
                                        <option key={category.id} value={category.id}>
                                            {category.name}
                                        </option>
                                    ))}
                                </select>

                                <div
                                    className={
                                        transaction.amount < 0
                                            ? "text-right font-medium text-emerald-400"
                                            : "text-right font-medium text-white"
                                    }
                                >
                                    {transaction.amount < 0 ? "+" : "-"}
                                    {formatCurrency(transaction.amount)}
                                </div>
                            </div>
                        ))}

                        {transactions.length === 0 ? (
                            <div className="px-4 py-12 text-center text-sm text-slate-400">
                                No transactions found.
                            </div>
                        ) : null}
                    </div>
                </div>
            </section>
        </main>
    );
}
