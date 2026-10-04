"use client";

import { useAuth } from "@clerk/nextjs";
import { useEffect } from "react";
import { setSessionTokenGetter } from "@/lib/api/session-token";

/**
 * Registers Clerk's session token for API calls.
 * Clears the getter on unmount and renders nothing.
 */
export function SessionTokenRegistration() {
  const { getToken } = useAuth();

  useEffect(() => {
    setSessionTokenGetter(() => getToken());
    return () => setSessionTokenGetter(null);
  }, [getToken]);

  return null;
}
