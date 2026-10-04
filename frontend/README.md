# Cardui frontend

Next.js 16 / React 19 frontend for Cardui.

Follow the repository root [`README.md`](../README.md) for prerequisites, local
database and API setup, and the complete startup sequence.

## Local configuration

Create `frontend/.env.local`:

```text
API_BASE_URL=http://localhost:5235
NEXT_PUBLIC_API_BASE_URL=http://localhost:5235
```

These local URLs are not secrets. Do not put credentials or private financial data in
frontend environment variables; values prefixed with `NEXT_PUBLIC_` are exposed to the
browser.

## Commands

Run these commands from `frontend/`:

```powershell
pnpm install --frozen-lockfile
pnpm dev
```

The application is available at `http://localhost:3000` and expects the local API at
`http://localhost:5235`.

Verification:

```powershell
pnpm test
pnpm lint
pnpm format:check
pnpm build
```

Before changing Next.js behavior, read `AGENTS.md` and the relevant version-matched
guidance under `node_modules/next/dist/docs/`.
