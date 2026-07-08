import axios from "axios";

export type ApiError = {
    status?: number;
    message: string;
    originalError: unknown;
};

export const apiClient = axios.create({
    baseURL: process.env.NEXT_PUBLIC_API_BASE_URL,
    timeout: 10_000,
    headers: {
        "Content-Type": "application/json",
    },
});

function getErrorMessageFromResponseData(data: unknown): string | undefined {
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

apiClient.interceptors.request.use((config) => {
    return config;
});

apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        const status = error.response?.status;
        const message =
            getErrorMessageFromResponseData(error.response?.data) ??
            error.message ??
            "Something went wrong.";

        return Promise.reject({
            status,
            message,
            originalError: error,
        } satisfies ApiError);
    },
);