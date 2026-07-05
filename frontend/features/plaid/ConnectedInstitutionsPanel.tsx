"use client";

import { useState } from "react";
import {
    syncPlaidItem,
    type PlaidItemDto,
    type SyncPlaidItemResponseDto,
} from "@/lib/api/plaid";

type ConnectedInstitutionsPanelProps = {
    initialItems: PlaidItemDto[];
};

function formatSyncedAt(value: string | null) {
    if (!value) {
        return "Never synced";
    }

    return new Intl.DateTimeFormat("en-US", {
        month: "short",
        day: "numeric",
        hour: "numeric",
        minute: "2-digit",
    }).format(new Date(value));
}

export function ConnectedInstitutionsPanel({initialItems,}: ConnectedInstitutionsPanelProps) {
    const [items, setItems] = useState(initialItems);
    const [syncingItemId, setSyncingItemId] = useState<string | null>(null);
    const [lastResult, setLastResult] =
        useState<SyncPlaidItemResponseDto | null>(null);

    async function handleSync(plaidItemId: string) {
        setSyncingItemId(plaidItemId);
        setLastResult(null);

        try {
            const result = await syncPlaidItem(plaidItemId);
            setLastResult(result);

            setItems((currentItems) =>
                currentItems.map((item) =>
                    item.id === plaidItemId
                        ? {
                            ...item,
                            updatedAt: new Date().toISOString(),
                            lastTransactionsSyncedAt: new Date().toISOString(),
                        }
                        : item,
                ),
            );

            window.location.reload();
        } finally {
            setSyncingItemId(null);
        }
    }

    return (
        <section className="rounded-lg border border-slate-800 bg-slate-900">
            <div className="border-b border-slate-800 p-4">
                <h2 className="font-semibold">Connected institutions</h2>
            </div>

            <div className="divide-y divide-slate-800">
                {items.map((item) => (
                    <div
                        key={item.id}
                        className="flex flex-col gap-3 p-4 md:flex-row md:items-center md:justify-between"
                    >
                        <div>
                            <p className="font-medium">
                                {item.institutionName ?? "Connected institution"}
                            </p>
                            <p className="mt-1 text-sm text-slate-400">
                                Last synced {formatSyncedAt(item.lastTransactionsSyncedAt)}
                            </p>
                        </div>

                        <button
                            type="button"
                            disabled={syncingItemId === item.id}
                            onClick={() => handleSync(item.id)}
                            className="rounded-md bg-emerald-500 px-4 py-2 text-sm font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                            {syncingItemId === item.id ? "Syncing" : "Sync now"}
                        </button>
                    </div>
                ))}

                {items.length === 0 ? (
                    <div className="p-4 text-sm text-slate-400">
                        No institutions connected yet.
                    </div>
                ) : null}
            </div>

            {lastResult ? (
                <div className="border-t border-slate-800 p-4 text-sm text-slate-400">
                    Added {lastResult.transactions.added}, modified{" "}
                    {lastResult.transactions.modified}, removed{" "}
                    {lastResult.transactions.removed}.
                </div>
            ) : null}
        </section>
    );
}