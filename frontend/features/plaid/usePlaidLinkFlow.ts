"use client";

import { useCallback, useEffect, useState } from "react";
import { usePlaidLink, type PlaidLinkOnSuccessMetadata } from "react-plaid-link";
import {
  createPlaidLinkToken,
  exchangePlaidPublicToken,
} from "@/lib/api/plaid";

type UsePlaidLinkFlowOptions = {
  onSuccess?: () => void;
};

export function usePlaidLinkFlow(options: UsePlaidLinkFlowOptions = {}) {
  const { onSuccess: onSuccessCallback } = options;
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
        onSuccessCallback?.();
      } finally {
        setIsExchangingToken(false);
      }
    },
    [onSuccessCallback],
  );

  const { open, ready } = usePlaidLink({
    token: linkToken,
    onSuccess,
  });

  const isLoading = isCreatingToken || isExchangingToken;
  const isReady = ready && !isLoading;

  return {
    open,
    isReady,
    isCreatingToken,
    isExchangingToken,
    isLoading,
  };
}
