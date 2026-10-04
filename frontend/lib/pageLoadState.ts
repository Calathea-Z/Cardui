/**
 * Result of loading one page on the server.
 * `error` is set when the API did not answer. `data` is still safe to render.
 */
export type PageLoadState<T> = {
  data: T;
  error: string | null;
};
