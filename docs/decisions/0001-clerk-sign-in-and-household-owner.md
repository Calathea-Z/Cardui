# 0001. Clerk Hobby sign-in and one household per owner

Status: Accepted
Date: 2026-10-03
Updated: 2026-10-07

## Context

Phase 1 needed sign-in and household ownership before any data could be
isolated or shared.

## Decision

Sign-in uses Clerk Hobby. The API verifies the Clerk session token and
creates one household per signed-in owner. Every financial read and write
uses the household id resolved on the server. Contributors are household
facts, without partner invitations.

## Consequences

Multifactor authentication, passkeys, a configurable session lifetime, and
removing Clerk branding wait for a Pro upgrade. Partner access is Phase 7.

Source: `docs/reviews/archive/phase-1/2026-10-03-007-phase-1-sign-in-decision.md`,
`docs/reviews/archive/phase-1/2026-10-03-008-clerk-household-owner.md`,
`docs/reviews/archive/phase-1/2026-10-03-009-household-scope.md`.
