"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  usePlaidLink,
  type PlaidLinkError,
  type PlaidLinkOnSuccessMetadata,
} from "react-plaid-link";
import {
  createPlaidLinkToken,
  createPlaidUpdateLinkToken,
  exchangePlaidPublicToken,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";

type LinkIntent = { kind: "connect" } | { kind: "update"; plaidItemId: string };

type UsePlaidLinkFlowOptions = {
  onSuccess?: () => void;
  /**
   * Runs after the person finishes repairing an existing connection.
   * The public token from that Link session is not exchanged. The access token stays the same.
   */
  onUpdated?: (plaidItemId: string) => Promise<void> | void;
  onError?: (message: string) => void;
  enabled?: boolean;
};

type PlaidConnectStep = {
  isCreatingToken: boolean;
  isExchangingToken: boolean;
  errorMessage: string | null;
};

/**
 * The label on a button that starts a new bank connection.
 * Preparing and connecting name the wait. A failure says to try again.
 */
export function plaidConnectLabel(step: PlaidConnectStep) {
  if (step.isCreatingToken) {
    return "Preparing Plaid";
  }

  if (step.isExchangingToken) {
    return "Connecting";
  }

  if (step.errorMessage) {
    return "Try again";
  }

  return "Connect account";
}

/**
 * Connects a bank through Plaid Link, or repairs one that already exists.
 * A new connection exchanges the public token. A repair does not, because that access token does not change.
 */
export function usePlaidLinkFlow(options: UsePlaidLinkFlowOptions = {}) {
  const {
    onSuccess: onSuccessCallback,
    onUpdated,
    onError,
    enabled = true,
  } = options;
  const [linkToken, setLinkToken] = useState<string | null>(null);
  const [isCreatingToken, setIsCreatingToken] = useState(false);
  const [isExchangingToken, setIsExchangingToken] = useState(false);
  const [pendingUpdateId, setPendingUpdateId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [errorScope, setErrorScope] = useState<"connect" | "update" | null>(
    null,
  );
  const pendingOpenRef = useRef(false);
  const updatingRef = useRef(false);
  const prepareRequest = useRef(0);
  const intentRef = useRef<LinkIntent>({ kind: "connect" });
  const tokenKindRef = useRef<LinkIntent | null>(null);
  const onSuccessRef = useRef(onSuccessCallback);
  const onUpdatedRef = useRef(onUpdated);
  const onErrorRef = useRef(onError);

  useEffect(() => {
    onSuccessRef.current = onSuccessCallback;
    onUpdatedRef.current = onUpdated;
    onErrorRef.current = onError;
  }, [onSuccessCallback, onUpdated, onError]);

  const clearError = useCallback(() => {
    setErrorMessage(null);
    setErrorScope(null);
  }, []);

  /**
   * Stores a Link failure and tells the caller when one is listening.
   * The scope is the connection or the repair that was in progress.
   */
  const reportError = useCallback((message: string) => {
    setErrorMessage(message);
    setErrorScope(intentRef.current.kind);
    onErrorRef.current?.(message);
  }, []);

  /**
   * Requests a Plaid Link token for a new bank or for one existing connection.
   * A repair token is not reused. Returns false when the flow is disabled or the request fails.
   */
  const prepareLinkToken = useCallback(
    async (intent: LinkIntent) => {
      if (!enabled) {
        return false;
      }

      const request = prepareRequest.current + 1;
      prepareRequest.current = request;
      setIsCreatingToken(true);
      setLinkToken(null);
      tokenKindRef.current = null;
      clearError();

      try {
        const response =
          intent.kind === "update"
            ? await createPlaidUpdateLinkToken(intent.plaidItemId)
            : await createPlaidLinkToken();
        if (prepareRequest.current !== request) {
          return false;
        }

        intentRef.current = intent;
        tokenKindRef.current = intent;
        setLinkToken(response.linkToken);
        return true;
      } catch (error) {
        if (prepareRequest.current === request) {
          reportError(
            getApiErrorMessage(error, "Could not prepare Plaid Link."),
          );
        }

        return false;
      } finally {
        if (prepareRequest.current === request) {
          setIsCreatingToken(false);
        }
      }
    },
    [clearError, enabled, reportError],
  );

  /**
   * Finishes Link.
   * A new connection exchanges the public token. A repair ignores that token and runs the caller's update.
   */
  const onSuccess = useCallback(
    async (publicToken: string, metadata: PlaidLinkOnSuccessMetadata) => {
      const intent = intentRef.current;
      clearError();

      if (intent.kind === "update") {
        updatingRef.current = true;
        setLinkToken(null);
        tokenKindRef.current = null;
        try {
          await onUpdatedRef.current?.(intent.plaidItemId);
        } catch (error) {
          reportError(
            getApiErrorMessage(error, "Reconnect could not finish. Try again."),
          );
        } finally {
          updatingRef.current = false;
          setPendingUpdateId(null);
        }

        return;
      }

      setIsExchangingToken(true);

      try {
        await exchangePlaidPublicToken({
          publicToken,
          institutionId: metadata.institution?.institution_id,
          institutionName: metadata.institution?.name,
        });
        onSuccessRef.current?.();
      } catch (error) {
        reportError(getApiErrorMessage(error, "Could not connect account."));
      } finally {
        setIsExchangingToken(false);
      }
    },
    [clearError, reportError],
  );

  /**
   * Records a Link error when the person leaves before finishing.
   * Closing Link without an error is not a failure. A repair that already succeeded is left to finish its sync.
   */
  const onExit = useCallback(
    (error: PlaidLinkError | null) => {
      if (updatingRef.current) {
        return;
      }

      pendingOpenRef.current = false;
      setPendingUpdateId(null);
      if (!error) {
        return;
      }

      const message = error.display_message.trim()
        ? error.display_message
        : error.error_message.trim()
          ? error.error_message
          : "Plaid Link could not finish. Try again.";
      reportError(message);
    },
    [reportError],
  );

  const { open, ready } = usePlaidLink({
    token: enabled ? linkToken : null,
    onSuccess,
    onExit,
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
   * Opens Plaid Link for a new bank.
   * Requests a token first when the current one is missing or was made for a repair.
   */
  const openPlaid = useCallback(async () => {
    if (!enabled) {
      return;
    }

    clearError();
    const intent: LinkIntent = { kind: "connect" };
    intentRef.current = intent;

    if (tokenKindRef.current?.kind === "connect" && linkToken && ready) {
      open();
      return;
    }

    pendingOpenRef.current = true;
    const prepared = await prepareLinkToken(intent);
    if (!prepared) {
      pendingOpenRef.current = false;
    }
  }, [clearError, enabled, linkToken, open, prepareLinkToken, ready]);

  /**
   * Opens Plaid Link to repair one existing bank connection.
   * Each click asks for a new update-mode token for that institution.
   */
  const openUpdate = useCallback(
    async (plaidItemId: string) => {
      if (!enabled || pendingUpdateId) {
        return;
      }

      clearError();
      const intent: LinkIntent = { kind: "update", plaidItemId };
      intentRef.current = intent;
      setPendingUpdateId(plaidItemId);
      pendingOpenRef.current = true;
      const prepared = await prepareLinkToken(intent);
      if (!prepared) {
        pendingOpenRef.current = false;
        setPendingUpdateId(null);
      }
    },
    [clearError, enabled, pendingUpdateId, prepareLinkToken],
  );

  const isLoading =
    isCreatingToken || isExchangingToken || pendingUpdateId !== null;

  return {
    open: openPlaid,
    openUpdate,
    pendingUpdateId,
    isReady: Boolean(linkToken) && ready && !isLoading,
    canAttemptConnect: enabled && !isLoading,
    isCreatingToken,
    isExchangingToken,
    isLoading,
    errorMessage,
    connectErrorMessage: errorScope === "connect" ? errorMessage : null,
    clearError,
    retryPrepare: () => prepareLinkToken({ kind: "connect" }),
  };
}
