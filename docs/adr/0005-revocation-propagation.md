# ADR 0005: Revocation propagation, offline sites and stale grant detection

- Status: Accepted
- Date: 2026-11-12

## Context

A revoked card must stop working everywhere quickly, but sites decide locally ([ADR 0003](0003-access-decisions-at-the-edge.md)) and may be offline.

## Decision

### Connected sites: 5 seconds (p99)

| Step | Latency |
| --- | --- |
| Cardholder commits the card and the `CardRevoked` outbox row together | one commit |
| Outbox delivery to RabbitMQ | about 250 ms (query delay) |
| AccessControl projection appends the change | one commit |
| The replica's poller picks it up | up to 500 ms |
| Written to the open stream; the site applies it | milliseconds |

`accesshub.access_list.propagation.duration` measures from the event's `OccurredAt` to the stream write. An integration test revokes a card and asserts the change arrives on an open stream within 5 seconds. Measured on the live system: 201 ms.

### Offline sites: accepted degradation

An offline site keeps deciding on its last list and does not see the revocation until it reconnects. It then receives every missed change in order. Two things limit the damage:

1. **Visibility.** Operators see each site's connection state, last-seen time and lag (changes not yet delivered) on the dashboard and in `accesshub.sites.lag`.
2. **Stale grant detection.** AccessControl consumes the `AccessGranted` batches of `DoorEventsRecorded` ([ADR 0011](0011-batched-door-events.md)). If the cloud's model shows the card was already revoked or expired, or the cardholder already inactive, *at the event's `OccurredAt`*, it publishes `StaleAccessGranted` through the outbox and Alarm raises a high-priority alarm.

Detection fires once per source event ID. It is keyed by a `stale_grant_detections` row and not only by the inbox, so the same grant arriving under a new message ID cannot alert twice.

## Known limitation

Detection uses what AccessControl knows when it processes the grant. If the grant is processed **before** the revocation reaches AccessControl, it is not flagged. This happens when a site reconnects and uploads the grant while the revocation is still in the broker.

Closing the gap means re-checking recent grants whenever a revocation arrives, which requires AccessControl to keep a window of grants. It is deferred because the window is seconds wide in normal operation.

## Alternative rejected

**Make the site ask the cloud before granting.** This would close the offline window and put the cloud in the door-open path, which ADR 0003 rules out.

## Consequences

- The 5-second bound holds only for connected sites. The dashboard makes the exceptions visible.
- Sites report the access-list sequence they decided on, so an investigation can tell "stale list" from "wrong rule".
