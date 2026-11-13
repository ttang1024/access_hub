# ADR 0002: Database per service

- Status: Accepted
- Date: 2026-11-12

## Context

Services that share tables are coupled through the schema: one service's migration can break another, and row ownership becomes unclear. AccessControl must also keep working when Cardholder is unavailable, which it cannot do if it reads Cardholder's tables.

## Decision

Each service has its own PostgreSQL database (`cardholderdb`, `accesscontroldb`, `ingestiondb`, `alarmdb`), its own EF Core migrations, and its own MassTransit outbox and inbox tables. No service reads another's database.

Data owned elsewhere arrives only as integration events and is kept in the consumer's own read model: AccessControl's `cards` and `cardholders` tables, EventIngestion's site key hashes.

Migrations run at startup in Development only. Elsewhere a Kubernetes Job runs the service with `--migrate`.

## Alternative rejected

**One database, a schema per service.** It keeps ownership clear on paper but not in practice: nothing stops a cross-schema join, and the services still share one failure domain and one connection limit.

## Consequences

- Read models are eventually consistent and must tolerate events arriving more than once and in any order. Their tables therefore have no foreign keys, and every upsert is guarded by the source aggregate's version.
- Cardholder events carry the full aggregate state, so a consumer can upsert from any single event without having seen earlier ones.
- A query that spans services, such as site names next to events, is answered from the consumer's own copy or composed in the UI.
- Four databases to back up and monitor. Locally they are four databases on one Postgres container.
