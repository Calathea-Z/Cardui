import type { NextConfig } from "next";

/**
 * True in the Next.js dev server.
 * Dev keeps `unsafe-eval` for the dev runtime and skips HSTS on local HTTP.
 */
const isDev = process.env.NODE_ENV !== "production";
/**
 * API origin allowed by the content security policy.
 * Local development falls back to the API's default port.
 */
const apiOrigin =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5235";
/**
 * Scripts the app is allowed to run.
 * Clerk, Cloudflare challenges, and Plaid Link are required for sign-in and bank linking.
 */
const scriptSrc = [
  "'self'",
  "'unsafe-inline'",
  isDev ? "'unsafe-eval'" : "",
  "https://*.clerk.accounts.dev",
  "https://*.clerk.com",
  "https://challenges.cloudflare.com",
  "https://cdn.plaid.com",
]
  .filter(Boolean)
  .join(" ");

/**
 * Content security policy for pages, API calls, images, and embedded sign-in or Plaid frames.
 */
const contentSecurityPolicy = [
  "default-src 'self'",
  `script-src ${scriptSrc}`,
  `connect-src 'self' ${apiOrigin} https://localhost:7080 https://*.clerk.accounts.dev https://*.clerk.com https://clerk-telemetry.com https://*.plaid.com https://cdn.plaid.com`,
  "img-src 'self' data: blob: https://img.clerk.com https://*.clerk.com",
  "style-src 'self' 'unsafe-inline'",
  "font-src 'self' data:",
  "frame-src 'self' https://*.clerk.accounts.dev https://*.clerk.com https://challenges.cloudflare.com https://cdn.plaid.com",
  "worker-src 'self' blob:",
  "frame-ancestors 'none'",
  "base-uri 'self'",
  "form-action 'self'",
  "object-src 'none'",
].join("; ");

/**
 * Response headers that limit framing, sniffing, and device permissions.
 * HSTS is production-only because local HTTP cannot satisfy it.
 */
const securityHeaders = [
  { key: "Content-Security-Policy", value: contentSecurityPolicy },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "X-Frame-Options", value: "DENY" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  {
    key: "Permissions-Policy",
    value: "camera=(), microphone=(), geolocation=()",
  },
  ...(isDev
    ? []
    : [
        {
          key: "Strict-Transport-Security",
          value: "max-age=31536000; includeSubDomains",
        },
      ]),
];

/**
 * Next.js config.
 * Hides the framework header and sends the security headers on every route.
 */
const nextConfig: NextConfig = {
  poweredByHeader: false,
  /**
   * Applies the content security policy and related headers to every path.
   */
  async headers() {
    return [
      {
        source: "/:path*",
        headers: securityHeaders,
      },
    ];
  },
};

export default nextConfig;
