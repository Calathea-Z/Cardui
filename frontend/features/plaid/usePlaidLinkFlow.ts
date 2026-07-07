"use client";

import { useCallback, useEffect, useState } from "react";
import { usePlaidLink, type PlaidLinkOnSuccessMetadata } from "react-plaid-link";
import { getApiErrorMessage } from "@/lib/api";
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
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const clearError = useCallback(() => {
    setErrorMessage(null);
  }, []);

  const prepareLinkToken = useCallback(async () => {
    setIsCreatingToken(true);
    clearError();

    try {
      const response = await createPlaidLinkToken();
      setLinkToken(response.linkToken);
      return true;
    } catch (error) {
      setErrorMessage(
        getApiErrorMessage(error, "Could not prepare Plaid Link."),
      );
      return false;
    } finally {
      setIsCreatingToken(false);
    }
  }, [clearError]);

  useEffect(() => {
    let cancelled = false;

    async function loadInitialLinkToken() {
      setIsCreatingToken(true);

      try {
        const response = await createPlaidLinkToken();
        if (!cancelled) {
          setLinkToken(response.linkToken);
        }
      } catch (error) {
        if (!cancelled) {
          setErrorMessage(
            getApiErrorMessage(error, "Could not prepare Plaid Link."),
          );
        }
      } finally {
        if (!cancelled) {
          setIsCreatingToken(false);
        }
      }
    }

    void loadInitialLinkToken();

    return () => {
      cancelled = true;
    };
  }, []);

  const onSuccess = useCallback(
    async (publicToken: string, metadata: PlaidLinkOnSuccessMetadata) => {
      setIsExchangingToken(true);
      clearError();

      try {
        await exchangePlaidPublicToken({
          publicToken,
          institutionId: metadata.institution?.institution_id,
          institutionName: metadata.institution?.name,
        });
        onSuccessCallback?.();
      } catch (error) {
        setErrorMessage(
          getApiErrorMessage(error, "Could not connect account."),
        );
      } finally {
        setIsExchangingToken(false);
      }
    },
    [onSuccessCallback, clearError],
  );

  const { open, ready } = usePlaidLink({
    token: linkToken,
    onSuccess,
  });

  const openPlaid = useCallback(() => {
    clearError();

    if (!linkToken) {
      void prepareLinkToken();
      return;
    }

    open();
  }, [clearError, linkToken, open, prepareLinkToken]);

  const isLoading = isCreatingToken || isExchangingToken;
  const isReady = ready && !isLoading;
  const canAttemptConnect = isReady || Boolean(errorMessage);

  return {
    open: openPlaid,
    isReady,
    canAttemptConnect,
    isCreatingToken,
    isExchangingToken,
    isLoading,
    errorMessage,
    clearError,
    retryPrepare: prepareLinkToken,
  };
}
