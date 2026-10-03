import "server-only";

import { auth } from "@clerk/nextjs/server";
import axios from "axios";
import {
  getErrorMessageFromResponseData,
  type ApiError,
} from "./errors";

const baseURL = process.env.API_BASE_URL;

if (!baseURL) {
  throw new Error(
    "API_BASE_URL is not set. Set it in frontend/.env for server-side API calls.",
  );
}

export const serverClient = axios.create({
  baseURL,
  timeout: 10_000,
  headers: {
    "Content-Type": "application/json",
  },
});

serverClient.interceptors.request.use(async (config) => {
  const { getToken } = await auth();
  const token = await getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

serverClient.interceptors.response.use(
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
