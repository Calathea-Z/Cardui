Date: 2026-10-17
Status: Awaiting review
PR: None

# Phone-access proposal

## Increment

Core journey item 2. This proposes how Zach's phone reaches the local app.
It does not install software, change bind addresses, CORS, content security
policy, Clerk settings, or authorized origins, and it does not publish
startup commands. The sequence and acceptance contract are approved in
[`2026-10-17-004-core-journey-sequence.md`](2026-10-17-004-core-journey-sequence.md).

## Proposed setup

Use Tailscale Serve on the development computer, limited to his tailnet.
Do not use Tailscale Funnel. Funnel would publish the same port on the
public internet.

The Next.js dev server stays on `127.0.0.1:3000`. The API stays on
`127.0.0.1:5235`. PostgreSQL stays on `127.0.0.1:5433` and is not served.
Serve only proxies HTTPS from the tailnet to those two loopback ports. The
processes do not bind to a public interface.

The phone browser needs two HTTPS origins, because the page calls the API
directly through `NEXT_PUBLIC_API_BASE_URL`:

- The frontend origin forwards to `127.0.0.1:3000`.
- A second tailnet HTTPS port forwards to `127.0.0.1:5235`.

Both names are the computer's tailnet DNS name, `*.ts.net`, not a public
hostname. The exact names are known only after Serve is enabled, so they
are not invented here.

Item 5, after this proposal is approved and the minimal chat exists, is the
increment that may change development configuration. That change is limited
to the exact frontend origin and the exact API origin: `API_BASE_URL`,
`NEXT_PUBLIC_API_BASE_URL`, `Cors:AllowedOrigins`, `Clerk:AuthorizedParties`,
the content security policy's API origin, and `AllowedHosts` if the proxy
forwards the tailnet host. No wildcard. The app stays in Development.
Clerk's dashboard would allow that same frontend origin for this
development instance. Production settings are not changed.

The phone walk uses a distinct test identity and a synthetic household.
Normal localhost startup stays as it is.

## Phone and browser

The repository does not record the phone or browser. Approval of this
proposal should name the phone's OS, version, browser, and version. The
setup is the same for a current phone browser. It does not require a native
app or a PWA.

## Access, data, and cost

Access is limited to devices on Zach's tailnet. A tailnet member who can
reach the computer can open the development app. Clerk still has to sign
them in. The database is not on the tailnet.

Use a personal-account tailnet. Tailscale's Personal plan is $0 for
non-commercial use, up to 6 users, with user devices included. That price
is from Tailscale's pricing page on October 17, 2026. A tailnet created
with a custom domain can be treated as a business plan instead, so this
proposal uses a personal account. No Tailscale paid plan is requested.

## What this review does not do

No Tailscale install, HTTPS certificate, firewall change, or configuration
edit. No command is written into the README until item 5 has run the real
ones. No credential is committed.

## Verification

Documentation only. No device check, network change, or application change.
`git diff --check` on the edited docs passed.

## Approval

Awaiting review. Approval accepts Tailscale Serve, tailnet only, for the
later phone increment. It does not turn access on, and it does not start
item 3.

## Pending decision

Approve this setup, and name the phone OS and browser, or ask for a
different one. Item 5 waits for that approval. Item 3 is next only after
this proposal is approved.

## Correction

October 17, 2026. Desktop guided recovery and MX connectivity now come
before phone access. This proposal still awaits review, and it is not the
gate for household scoping. Nothing in the proposal was installed.
