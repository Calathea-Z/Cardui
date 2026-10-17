# Tortoise frontend

Next.js 16 / React 19 frontend for Tortoise.

Follow the repository root [`README.md`](../README.md) for prerequisites, local database and API setup, and the complete startup sequence. Create `frontend/.env.local` from the example in that README's [Setup](../README.md#setup) section.

`CLERK_SECRET_KEY` is a secret: keep it in that gitignored file and out of source control. Values prefixed with `NEXT_PUBLIC_` are exposed to the browser, so do not put credentials or private financial data in them. The local API URLs are not secrets.

## Commands

Run these commands from `frontend/`:

```powershell
pnpm install --frozen-lockfile
pnpm dev
```

The application is available at `http://localhost:3000` and expects the local API at `http://localhost:5235`.

Verification:

```powershell
pnpm test
pnpm lint
pnpm format:check
pnpm build
```

Before changing Next.js behavior, read [`frontend/AGENTS.md`](AGENTS.md) and the relevant version-matched guidance under `node_modules/next/dist/docs/`.
