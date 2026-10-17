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

Accepted October 3, 2026. The increment reports are in Git history.
