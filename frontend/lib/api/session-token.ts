let getSessionToken: (() => Promise<string | null>) | null = null;

/**
 * Registers how the browser client reads the Clerk session token.
 * Passing null clears it, which is what sign-out does.
 */
export function setSessionTokenGetter(
  getter: (() => Promise<string | null>) | null,
) {
  getSessionToken = getter;
}

/**
 * Reads the current Clerk session token for an API request.
 * Returns null before the app has registered a getter.
 */
export async function readSessionToken(): Promise<string | null> {
  if (!getSessionToken) {
    return null;
  }

  return getSessionToken();
}
