/**
 * Failure from an API call, with the HTTP status when the server answered.
 */
export type ApiError = {
  status?: number;
  message: string;
  originalError: unknown;
};

/**
 * Reads a Problem Details `detail` or a `message` from an error body.
 * Other shapes are ignored so the caller can use its own fallback.
 */
export function getErrorMessageFromResponseData(
  data: unknown,
): string | undefined {
  if (typeof data !== "object" || data === null) {
    return undefined;
  }

  if ("detail" in data && typeof data.detail === "string" && data.detail) {
    return data.detail;
  }

  if ("message" in data && typeof data.message === "string" && data.message) {
    return data.message;
  }

  return undefined;
}

/**
 * Returns a message safe to show for an unknown thrown value.
 */
export function getApiErrorMessage(
  error: unknown,
  fallback = "Something went wrong.",
): string {
  if (typeof error === "object" && error !== null && "message" in error) {
    const message = error.message;

    if (typeof message === "string" && message) {
      return message;
    }
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallback;
}
