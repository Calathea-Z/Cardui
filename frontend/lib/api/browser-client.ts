import "client-only";

import axios from "axios";
import { getErrorMessageFromResponseData, type ApiError } from "./errors";
import { readSessionToken } from "./session-token";

/**
 * Axios client for calls made in the browser.
 * Attaches the Clerk session token and turns HTTP failures into `ApiError`.
 */
export const browserClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_BASE_URL,
  timeout: 10_000,
  headers: {
    "Content-Type": "application/json",
  },
});

browserClient.interceptors.request.use(async (config) => {
  const token = await readSessionToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

browserClient.interceptors.response.use(
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
