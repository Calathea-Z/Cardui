let getSessionToken: (() => Promise<string | null>) | null = null;

export function setSessionTokenGetter(
  getter: (() => Promise<string | null>) | null,
) {
  getSessionToken = getter;
}

export async function readSessionToken(): Promise<string | null> {
  if (!getSessionToken) {
    return null;
  }

  return getSessionToken();
}
