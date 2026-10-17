Date: 2026-10-17
Status: Approved 2026-10-17
PR: None

# Mobile testing readiness

## Increment

Extend the user-journey assessment with an explicit real-device testing
milestone. Mobile access and core mobile usability should be ready before
the guided-planning pilot, not deferred wholesale to Phase 6. This report
plans the work; it does not expose services or implement mobile changes.

## Current gap

The documented local address cannot simply be opened on a phone:

- `frontend/package.json` starts Next.js bound to localhost.
- `api/Properties/launchSettings.json` binds the API to localhost.
- The README sets the browser API URL to `http://localhost:5235`. On a
  phone that address refers to the phone, not the development computer.
- Browser API calls use `NEXT_PUBLIC_API_BASE_URL`; server-side calls use
  their own configuration. Both paths must work in a device-testing setup.
- CORS defaults to localhost frontend origins. The frontend content security
  policy also uses the configured browser API origin. Authentication and
  redirects must be verified for the actual testing origin.
- `.cursor/rules/security.mdc` requires local app/database servers to bind
  to loopback. A network-access change needs an explicitly reviewed setup;
  merely changing the frontend hostname would be incomplete.

Prior phone-width desktop checks are useful layout evidence. They do not
establish physical-device access, touch behavior, software-keyboard behavior,
mobile browser authentication, or recovery after backgrounding the app.

## Proposed steps and order

1. **Record the target device.** Start with Zach's actual phone and its
   browser; record OS/browser versions. Add the other major mobile browser
   family before a broader alpha. Do not require a native app or PWA.
2. **Prepare one repeatable device-access setup.** Evaluate an authenticated
   HTTPS test origin forwarding to local services against an isolated hosted
   test environment. Prefer the smallest setup that works with the actual
   device and sign-in. Name the provider/destination, access restrictions,
   data exposure, and any cost before requesting setup approval. Do not
   assume that a tunnel itself provides authentication or that an HTTPS
   frontend can call a localhost/HTTP API from a phone.
3. **Connect the complete request path.** Configure browser and server API
   addresses, TLS, exact allowed origins, content security policy, and
   authentication redirects/authorized origins for the chosen setup. Verify
   page loading and client-side reads/writes after sign-in. Keep database
   access private and normal local startup unchanged. Use a distinct test
   identity and synthetic household; prefer an isolated test database for
   repeated data-changing acceptance runs.
4. **Document startup and shutdown.** Once implemented and verified, add
   actual commands, prerequisites, the phone URL, sign-in steps, test-data
   setup, troubleshooting, and how to stop access to the root README. Do not
   publish speculative commands or commit credentials. Include checks for
   a page that loads but cannot fetch data, failed sign-in redirects, and
   stale frontend API configuration.
5. **Run and repair the existing mobile journey now.** Sign in, enter facts,
   inspect Plan, try a change, save/accept, and reopen. Keep changes limited
   to blockers in that journey. Use desktop narrow-width checks during
   development, followed by Zach's physical-device acceptance.
6. **Add chat-specific mobile acceptance when chat is connected.** Run the
   first-plan and return-session journeys from the previous assessment on
   the same phone setup. Mobile readiness is part of that completion gate.

Detailed technology selection and configuration verification belong to the
implementation increment. No hosting provider, dependency, public deployment,
firewall exception, or external data-sharing arrangement is selected here.

## Mobile UX checklist for Zach

These future checks create/change synthetic records, messages, and plan
versions. Live model checks require the separately agreed evaluation budget.

| Check | Expected result |
| --- | --- |
| Open the test URL, sign in, reload, sign out/in | Correct household loads; no redirect loop or broken API requests |
| Start with an empty household and enter cash, income, bills, and debt | Labels and errors are readable; touch controls and numeric/date entry work; no required field is hidden |
| Open and dismiss a sheet, picker, and detail panel | Controls remain reachable; scrolling works; return goes to the expected place |
| Read Plan and try one change | Main cash answer and next step are clear; no page-wide horizontal scroll; charts do not force unreadably small text |
| Save/accept and reopen a plan | State/version is understandable; saved result matches the reviewed calculation |
| Open chat with the software keyboard visible | Composer, send control, and latest reply remain reachable; navigation and safe areas do not cover them |
| Send, background the browser, and return | Pending/succeeded/failed state is explicit; no lost draft or duplicate submission |
| Interrupt connectivity and retry | Recoverable error preserves work; repeated taps do not create duplicate facts or accepted plans |
| Rotate, enlarge text, and navigate with VoiceOver or TalkBack | Content remains usable; controls have meaningful labels and reading/focus order |

Record device/browser, journey, result, severity, and reproduction steps.
Avoid screenshots or reports containing real financial data. Fix blockers
before adding mobile polish or more features. A desktop viewport pass does
not close physical-device checks.

## Exit and scope boundary

Zach can open the documented URL on his actual phone, sign in, finish the
manual planning journey without developer intervention, and reopen the saved
result. Once chat exists, that same gate includes guided intake, confirmation,
acceptance, and returning with a correction. Keyboard/connection interruptions
must preserve work. Broader device coverage and operational hardening remain
later release work. Native apps, installation prompts, push notifications,
and offline financial writes are outside this milestone.

## Verification

Read the README, frontend startup/API configuration, Next.js security headers,
API launch profiles, CORS configuration, and security rule. Documentation-only
change; no application tests or physical-device checks ran. No services,
credentials, network access, database records, or machine settings changed.
`git diff --check` passed.

## Approval

Approved by Zach on October 17, 2026 as a finding to incorporate. No device
check in this report was run, and this approval does not say one passed. The
revised sequence is in
[`2026-10-17-004-core-journey-sequence.md`](2026-10-17-004-core-journey-sequence.md)
and awaits review before implementation. Target phone, browser, and access
setup remain unselected.
