# ADR 0006: Transactional outbox and inbox; at-least-once delivery with idempotent consumers

- Status: Accepted
- Date: 2026-11-12

## Context

Postgres and RabbitMQ cannot commit together. Publishing after the database commit loses the event if the process dies in between; publishing before it announces a change that may never commit. Brokers also redeliver, and consumers restart mid-message.

## Decision

**Outbox.** Every integration event goes through the MassTransit EF Core outbox. Domain events are dispatched inside `SaveChangesAsync`, *before* commit. Their handlers map them to integration events, which become outbox rows in the same transaction as the state change and its audit entry. Nothing is published outside the outbox. EventIngestion's raw batch insert uses `IUnitOfWork.ExecuteInTransactionAsync` so the insert and its outbox rows commit together.

**Inbox.** Every consumer endpoint that writes to a database uses the EF Core inbox, which deduplicates redelivered messages by message ID, at `READ COMMITTED`. The one exception is the live feed, which writes nothing ([ADR 0010](0010-type-routed-door-events-and-stateless-live-feed.md)).

**Natural idempotency as well.** The inbox only recognises a repeated message ID, and the same fact can arrive under a new one. So each consumer is also idempotent by construction:

| Consumer | How a repeat becomes a no-op |
| --- | --- |
| Read models (AccessControl, EventIngestion key hashes) | Upsert by business ID, applied only when the incoming `Version` is greater |
| AccessControl change log | Entry diffing appends nothing for an unchanged entry |
| Ingestion | `ON CONFLICT (site_id, event_id) DO NOTHING`; only inserted rows are published |
| Stale grant detection | Keyed by source event ID |
| Alarm rules | Stored denials; the saga correlates to the open alarm per subject |

**Failures.** Six retries with exponential backoff (200 ms to 30 s), then the error queue. Consumers never swallow exceptions.

## Alternative rejected

**Publish after commit and reconcile later.** It needs a reconciliation job that knows every event type, and until it runs a lost `CardRevoked` means a site keeps opening doors for a revoked card.

## Consequences

- Delivery is at least once and may be out of order. Every consumer is written and tested for that with the MassTransit test harness: the same message twice, the same event under a new ID, versions out of order.
- Publish latency includes the outbox delivery delay (about 250 ms).
- Outbox and inbox tables need cleanup, done by MassTransit's built-in services with a 7-day duplicate detection window.
- One instance's outbox delivers in order with publisher confirms, which caps its throughput. [ADR 0011](0011-batched-door-events.md) is the result of hitting that cap.
