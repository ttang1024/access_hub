# Measured results

Measured 2026-11-12 and 2026-11-13 on one Apple M3 laptop (8 cores), running the whole system through the local AppHost: Postgres, RabbitMQ, Redis, Keycloak, five services, the simulator (3 sites) and the dashboard. Docker Desktop's VM, every .NET process and the load generator (k6 in Docker) shared the same 8 cores, so these numbers are a floor for the design, not a capacity figure.

## Targets

| Target | Result | Verdict |
| --- | --- | --- |
| A revocation reaches a connected site within 5 s | **201 ms** from the API call returning to the revoked entry being in the site's SQLite copy (applied sequence 46 → 47). The card was granted on sequence 46 and denied `CardRevoked` on 47. | Met |
| A forced-door event appears on the dashboard within 2 s | Through the gateway's SignalR hub (WebSocket): **68 ms** for the site event, **130 ms** for the alarm. | Met |
| A local decision takes under 1 ms (p99) with 10,000 cards | Asserted by a unit test (`Decide_TenThousandCards_TakesUnderAMillisecond`). The live `accesshub.access_decision.duration` histogram was not exported for this report. | Met by test only |
| With a 60 s outage, sites keep deciding, show as disconnected, and every event arrives after reconnect | All three sites showed disconnected, kept deciding, buffered up to 177 events each, reconnected with jittered backoff and drained their buffers to zero. No events were rejected. | Met |
| Stopping RabbitMQ for 60 s loses no events | Broker stopped for 70 s. Ingestion kept storing events and queued 385 messages in its outbox. 31 s after the restart the outbox was empty and all 123 `AccessDenied` events stored since the stop had reached Alarm. | Met |
| Ingestion: 10 batches of 200 events/s for 2 min, p99 under 250 ms, no errors | 1,200 batches (240,000 events) per run, **0 errors**, **p99 241 ms and 71 ms** on two warm runs, median about 17 ms. Outbox and queues stayed at about zero. Against freshly started services: p99 331 ms. | Met warm, missed cold |

Not measured: stopping each service individually for 60 seconds. Postgres was restarted once mid-run and every service recovered on its own.

## How the ingestion target was reached

k6 runs of `tests/load/ingest-events.js`, in order. Each row adds one change to the row above.

| Run | Change | Median | p95 | p99 |
| --- | --- | --- | --- | --- |
| 1 | Baseline: Debug build, every consumer receives every `DoorEventRecorded` | 146 ms | 455 ms | 896 ms |
| 2 | Events routed by type to the consumers that need them ([ADR 0010](adr/0010-type-routed-door-events-and-stateless-live-feed.md)) | 176 ms | 449 ms | 670 ms |
| 3 | Npgsql auto-prepare | 121 ms | 284 ms | 654 ms |
| 4 | Release build | 117 ms | 267 ms | 376 ms |
| 5 | Three ingestion replicas (later reverted locally) | 289 ms | 6.6 s | 16.2 s |
| 6 | One message per upload and event type ([ADR 0011](adr/0011-batched-door-events.md)), one replica | 120 ms | 296 ms | 488 ms |
| 7 | Unique card numbers in the load script | 124 ms | 285 ms | 603 ms |
| 8 | Set-based batch consumers, services cold | 24 ms | 143 ms | 331 ms |
| 9 | Same, warm | 17 ms | 57 ms | **241 ms** |
| 10 | Same, warm, repeated | 18 ms | 40 ms | **71 ms** |

The two warm runs differ by a factor of three at p99 (241 ms and 71 ms). On a machine shared with the load generator, the tail is noisy; the median and p95 are the stable figures.

## What the runs showed

**The per-event message was the cost, not the insert (runs 1 to 4).** Storing 200 events takes 10 to 30 ms. Writing 200 outbox rows through EF took 60 to 150 ms, or 20 to 90 ms once prepared. One instance's outbox then delivers in order with publisher confirms, about 600 messages/s here against 2,000 produced, so a backlog built during each run and drained afterwards. Nothing was lost.

**Scaling out moved the bottleneck (run 5).** Three replicas reached about 1,100 messages/s of delivery, which outpaced the consumers. Their inbox transactions saturated the single shared Postgres, the .NET processes were starved of CPU, and Postgres showed sessions idle in transaction waiting on its clients. The same run exhausted Postgres' 100 connections. The local AppHost therefore runs one ingestion replica; the production overlay runs three, each with its own share of a real database.

**Batching removed the bottleneck, but only once consumers were set-based (runs 6 to 8).** One message per upload and type cut outbox rows from 200 to at most six. Latency did not fall until the consumers stopped querying per event and worked on the whole batch in two queries.

**An alarm storm is a different test (runs 6 and 7).** The original load script drew from a pool of 10,000 cards at 500 denials/s, which tripped the repeated-denials rule for thousands of cards at once. The alarm saga is lock-bound in that scenario. It is not covered by a target and remains open.

## Fixes kept from this work

- Type-routed door events and a stateless live feed (ADR 0010).
- Batched door events and set-based consumers (ADR 0011).
- Npgsql auto-prepare on every service.
- Connection pools capped at 50 per instance; local Postgres `max_connections` raised to 300.
- A durable local RabbitMQ, so restarting the AppHost does not discard queued messages.
- Sync lag reported correctly after a snapshot.
- The uploader survives an open circuit breaker. The first outage run exposed this: the resilience pipeline's `BrokenCircuitException` killed the upload loop, which never resumed while the site kept buffering. Fixed and covered by a test.
