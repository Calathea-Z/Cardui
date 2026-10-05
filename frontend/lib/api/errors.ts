/**
 * Failure from an API call, with the HTTP status when the server answered.
 */
export type ApiError = {
  status?: number;
  message: string;
  originalError: unknown;
};

/**
 * Text safe to show after a failed API call.
 * On a 500, `message` is the sentence about the action and `detail` is the exception in development.
 * Any other status leaves the server text in `message`, including validation, and leaves `detail` empty.
 */
export type ApiErrorText = {
  message: string;
  detail?: string;
  isServerError: boolean;
};

const unexpectedServerDetail = "An unexpected error occurred.";

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
 * Turns a thrown API failure into the sentence to show.
 * A 500 uses the action sentence. Validation and other statuses keep their text.
 * In development, a 500 adds the exception as `detail`.
 */
export function describeApiError(
  error: unknown,
  fallback = "Something went wrong.",
  isDevelopment = process.env.NODE_ENV === "development",
): ApiErrorText {
  const serverMessage = readThrownMessage(error);

  if (isServerFailure(error)) {
    const detail =
      isDevelopment &&
      serverMessage &&
      serverMessage !== fallback &&
      serverMessage !== unexpectedServerDetail
        ? serverMessage
        : undefined;

    return {
      message: fallback,
      detail,
      isServerError: true,
    };
  }

  if (serverMessage) {
    return { message: serverMessage, isServerError: false };
  }

  return { message: fallback, isServerError: false };
}

/**
 * Returns a message safe to show for an unknown thrown value.
 * A 500 is one sentence about the action. In development the exception follows on the next line.
 * Validation text is returned unchanged.
 */
export function getApiErrorMessage(
  error: unknown,
  fallback = "Something went wrong.",
  isDevelopment = process.env.NODE_ENV === "development",
): string {
  const text = describeApiError(error, fallback, isDevelopment);

  return text.detail ? `${text.message}\n${text.detail}` : text.message;
}

/**
 * True when the response status is 500.
 * Other statuses keep their own text, including validation.
 */
function isServerFailure(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "status" in error &&
    error.status === 500
  );
}

/**
 * Reads a message from an API error or any thrown value that carries one.
 * An empty message is ignored so the caller can use its action sentence.
 */
function readThrownMessage(error: unknown): string | undefined {
  if (typeof error === "object" && error !== null && "message" in error) {
    const message = error.message;

    if (typeof message === "string" && message) {
      return message;
    }
  }

  return undefined;
}
