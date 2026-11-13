# ADR 0003: Access decisions at the edge; the cloud is not in the door-open path

- Status: Accepted
- Date: 2026-11-12

## Context

A door must open, or stay shut, within milliseconds whether or not the site's internet link is up. A cloud round trip adds latency and makes every door depend on the WAN, the gateway and AccessControl all being up.

## Decision

Each site controller holds a copy of its site's access list in SQLite and decides every swipe locally with `AccessDecisionEngine`, a pure function in `AccessControl.Domain`: no I/O, no clock, no logging.

The engine is the only domain code shared between the cloud and the edge. The cloud's "explain access" endpoint runs the same engine against the cloud's copy, so the two cannot disagree about the rules. An architecture test enforces that the simulator references only `AccessControl.Domain`, the contracts and ServiceDefaults.

Checks run in a fixed order and the first failure is the deny reason: unknown card, revoked, expired, cardholder inactive, no access to door, outside schedule (in the site's IANA time zone).

A site that has never synced fails closed: with no list, every card is `UnknownCard`.

## Alternatives rejected

- **Decide in the cloud.** Every door would stop working during a WAN outage.
- **Decide in the cloud, fall back to a local cache.** Two decision paths that can disagree, and the less-tested one runs exactly when things are already going wrong.

## Consequences

- Decisions take microseconds. A unit test asserts p99 under 1 ms with 10,000 cards, and the simulator records `accesshub.access_decision.duration`.
- A site decides on what it last received. An offline site does not see revocations until it reconnects; [ADR 0005](0005-revocation-propagation.md) covers how that window is bounded and detected.
- The engine must stay backward compatible with lists already on sites, and a change to it ships to every site controller.
