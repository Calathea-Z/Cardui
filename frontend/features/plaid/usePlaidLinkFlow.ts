"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  usePlaidLink,
  type PlaidLinkOnSuccessMetadata,
} from "react-plaid-link";
import {
  createPlaidLinkToken,
  exchangePlaidPublicToken,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";

type UsePlaidLinkFlowOptions = {
  onSuccess?: () => void;
  enabled?: boolean;
};

/**
 * Connects a bank through Plaid Link.
 * Creates a link token, opens Link when it is ready, and exchanges the public token after the user finishes.
 */
export function usePlaidLinkFlow(options: UsePlaidLinkFlowOptions = {}) {
  const { onSuccess: onSuccessCallback, enabled = true } = options;
  const [linkToken, setLinkToken] = useState<string | null>(null);
  const [isCreatingToken, setIsCreatingToken] = useState(false);
  const [isExchangingToken, setIsExchangingToken] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const pendingOpenRef = useRef(false);

  const clearError = useCallback(() => {
    setErrorMessage(null);
  }, []);

  /**
   * Requests a Plaid Link token.
   * Returns false when the flow is disabled or the request fails.
   */
  const prepareLinkToken = useCallback(async () => {
    if (!enabled) {
      return false;
    }

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
  }, [clearError, enabled]);

  /**
   * Exchanges the Plaid public token for a bank connection.
   * Passes the institution id and name from Plaid when they are present.
   */
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
    token: enabled ? linkToken : null,
    onSuccess,
  });

  useEffect(() => {
    if (!enabled) {
      pendingOpenRef.current = false;
      return;
    }

    if (!pendingOpenRef.current || !linkToken || !ready) {
      return;
    }

    pendingOpenRef.current = false;
    open();
  }, [enabled, linkToken, ready, open]);

  /**
   * Opens Plaid Link.
   * Requests a token first when one is missing, and opens once Link is ready.
   */
  const openPlaid = useCallback(async () => {
    if (!enabled) {
      return;
    }

    clearError();

    if (!linkToken) {
      pendingOpenRef.current = true;
      const prepared = await prepareLinkToken();
      if (!prepared) {
        pendingOpenRef.current = false;
      }
      return;
    }

    if (ready) {
      open();
      return;
    }

    pendingOpenRef.current = true;
  }, [clearError, enabled, linkToken, open, prepareLinkToken, ready]);

  const isLoading = isCreatingToken || isExchangingToken;

  return {
    open: openPlaid,
    isReady: Boolean(linkToken) && ready && !isLoading,
    canAttemptConnect: enabled && !isLoading,
    isCreatingToken,
    isExchangingToken,
    isLoading,
    errorMessage,
    clearError,
    retryPrepare: prepareLinkToken,
  };
}
