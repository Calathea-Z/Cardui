import type { NextConfig } from "next";

const isDev = process.env.NODE_ENV !== "production";
const apiOrigin =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5235";
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

const nextConfig: NextConfig = {
  poweredByHeader: false,
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
