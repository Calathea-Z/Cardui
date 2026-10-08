# Handoff roadmap order

Date: October 8, 2026
Status: Awaiting review
PR:

## Increment

Before a next-chat prompt is written, the handoff compares that next item with the roadmap. If the prompt would skip an earlier item that is still not started, it asks Zach whether he knows and wants to continue out of order. It asks again before every later prompt that would skip those items. The prompt is written only after he says to continue.

## Decision

The check lives in `.cursor/rules/handoff.mdc`, which already applies to every chat. `AGENTS.md` states the same check and points at that rule. `docs/reviews/README.md` points at the rule so a review does not invent a different handoff.

The plan is out of order when the prompt would skip an earlier item in the same phase, or an earlier phase item the roadmap still lists as not started. An inserted section the roadmap places before that earlier item is in order. A yes on one prompt does not cover the next handoff. If Zach says no, the prompt names the earliest open item.

On October 8, 2026, Phase 2 items 7 and 8 were still not started when a prompt said to start Phase 3. That is the case this rule is for. `docs/README.md` now lists Phase 2 item 7 as next, after this rule is approved. Item 8 waits for item 7. The rest of Phase 3 item 6 waits for both.

## Changes

- `.cursor/rules/handoff.mdc` asks before an out-of-order next-chat prompt, and asks again at each later handoff.
- `AGENTS.md` states that check in the review handoff.
- `docs/reviews/README.md` points the next-chat prompt at the handoff rule.

## Data changes

None.

## Agent verification

Documentation only. No application code, dependencies, database, or runtime behavior changed. Application tests were not run.

## Manual verification

No data changes. Read the wording.

1. Read `.cursor/rules/handoff.mdc` and the Review handoff section in `AGENTS.md`.
   Expected: both say to ask, before the next-chat prompt, when an earlier roadmap item is still not started, and to ask again before every later prompt that would skip it. The prompt is written only after you say to continue. A previous yes does not cover the next prompt.
2. Open `docs/README.md`.
   Expected: Now is this rule, awaiting review. Next is Phase 2 item 7 after approval, then item 8, then the rest of Phase 3 item 6.

## Approval

Awaiting Zach's review.

## Pending decision

None in this slice. After approval, the next roadmap item is Phase 2 item 7: an operating cash reserve, an emergency goal, and sinking funds.
