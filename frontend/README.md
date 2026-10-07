# Cardui frontend

Next.js 16 / React 19 frontend for Cardui.

Follow the repository root [`README.md`](../README.md) for prerequisites, local
database and API setup, and the complete startup sequence.

## Local configuration

Create `frontend/.env.local` with the local API URLs and the Clerk keys for this
application. The root [`README.md`](../README.md) has the PowerShell command that
writes it.

```text
API_BASE_URL=http://localhost:5235
NEXT_PUBLIC_API_BASE_URL=http://localhost:5235
NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY=<Clerk publishable key>
CLERK_SECRET_KEY=<Clerk secret key>
NEXT_PUBLIC_CLERK_SIGN_IN_URL=/sign-in
NEXT_PUBLIC_CLERK_SIGN_UP_URL=/sign-up
```

The local URLs are not secrets. `CLERK_SECRET_KEY` is a secret: keep it in this
gitignored file and out of source control. Values prefixed with `NEXT_PUBLIC_` are
exposed to the browser, so do not put credentials or private financial data in them.

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
