"use client";

import { useCallback, useEffect, useState } from "react";
import { usePlaidLink, type PlaidLinkOnSuccessMetadata } from "react-plaid-link";
import {
    createPlaidLinkToken,
    exchangePlaidPublicToken,
} from "@/lib/api/plaid";

export function PlaidLinkButton() {
    const [linkToken, setLinkToken] = useState<string | null>(null);
    const [isCreatingToken, setIsCreatingToken] = useState(true);
    const [isExchangingToken, setIsExchangingToken] = useState(false);

    useEffect(() => {
        async function loadLinkToken() {
            try {
                const response = await createPlaidLinkToken();
                setLinkToken(response.linkToken);
            } finally {
                setIsCreatingToken(false);
            }
        }

        loadLinkToken();
    }, []);

    const onSuccess = useCallback(
        async (publicToken: string, metadata: PlaidLinkOnSuccessMetadata) => {
            setIsExchangingToken(true);

            try {
                await exchangePlaidPublicToken({
                    publicToken,
                    institutionId: metadata.institution?.institution_id,
                    institutionName: metadata.institution?.name,
                });
            } finally {
                setIsExchangingToken(false);
            }
        },
        [],
    );

    const { open, ready } = usePlaidLink({
        token: linkToken,
        onSuccess,
    });

    return (
        <button
            type="button"
            disabled={!ready || isCreatingToken || isExchangingToken}
            onClick={() => open()}
            className="cursor-pointer rounded-md bg-emerald-500 px-4 py-2 text-sm font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
        >
            {isCreatingToken
                ? "Preparing Plaid"
                : isExchangingToken
                    ? "Connecting"
                    : "Connect account"}
        </button>
    );
}