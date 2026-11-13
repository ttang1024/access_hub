# ADR 0004: Access list sync: snapshot plus sequenced change stream

- Status: Accepted
- Date: 2026-11-12

## Context

Every site must converge on exactly the list the cloud holds for it, across restarts, outages and several AccessControl replicas, without downloading the whole list for every change.

## Decision

### Per-site change log

For every effective change to a site's list, AccessControl appends one row to `site_changes(site_id, sequence, kind, entity_id, payload | tombstone, changed_at, recorded_at)` in the same transaction as the read-model update. `site_entries` holds the current list.

**Effective changes only.** A projection recomputes what each affected site should hold for each affected entity and compares it, byte for byte, with `site_entries`. A redelivered or older-version event appends nothing. Neither does an edit that does not change a site's view: a schedule edit changes the group entry, not every card.

**Gap-free ordering.** `sequence` is per site and starts at 1. It is allocated by incrementing a counter on the site row, read with `SELECT ... FOR UPDATE`. The row lock is held until commit, so sequence N+1 cannot commit before N, and a reader polling `sequence > cursor` can never skip a change that commits late.

**No dirty computation.** Projections also take a transaction-scoped advisory lock and run at `READ COMMITTED`, so two events about the same cardholder never compute entries from each other's uncommitted state. `REPEATABLE READ` would not work here: its snapshot is taken before the lock is acquired, so the second transaction would read stale rows.

### gRPC `AccessListSync`

- `GetSnapshot` streams `site_entries` in chunks from one `REPEATABLE READ` snapshot. The last chunk carries the sequence it reflects.
- `StreamChanges(from)` subscribes to the replica's in-process feed first, then sends the backlog from the log, then live changes. Subscribing first means nothing can fall between backlog and live; duplicates are skipped and any hole is filled from the log. The client sees every sequence exactly once, in order.
- A heartbeat goes out every 15 seconds.
- A `from` older than the retained log, or ahead of it (a site restored from a newer state than the cloud), fails with `FAILED_PRECONDITION` and the site takes a new snapshot.
- Fan-out is one poller per replica, reading new rows every 500 ms for the sites it streams to. There is no cross-replica messaging.
- Changes are retained for 7 days, measured from `recorded_at` (append time), so a late-arriving old event is not pruned the moment it is appended.

### Site side

A change is applied only if its sequence is exactly applied + 1, and it is written with the new applied sequence in one SQLite transaction. On any gap the site reconnects from its applied sequence.

## Alternatives rejected

- **Sequence from a database `SEQUENCE` or identity column.** Values are handed out before commit, so a later value can become visible first, and a rolled-back transaction leaves a gap. A polling reader would skip changes.
- **Push changes to sites straight from the consumer.** The replica that consumes an event is not the one holding the site's stream, so this needs cross-replica messaging and still needs a log for sites that are offline.
- **Send the full list on every change.** Simple and self-healing, but the cost grows with list size times change rate.

## Consequences

- Propagation latency is bounded by the poll interval plus commit time, well under the 5-second target. An integration test asserts it.
- The sequence lock serialises writers per site. Cardholder events arrive at operator rate, so this is not a bottleneck.
- The log grows with change volume. Pruning plus re-snapshot keeps it bounded.
