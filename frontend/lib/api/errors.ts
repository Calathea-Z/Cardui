export type ApiError = {
  status?: number;
  message: string;
  originalError: unknown;
};

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

/** Narrow unknown rejection into ApiError shape when useful for callers. */
export function isApiError(error: unknown): error is ApiError {
  return (
    typeof error === "object" &&
    error !== null &&
    "message" in error &&
    typeof (error as ApiError).message === "string"
  );
}
