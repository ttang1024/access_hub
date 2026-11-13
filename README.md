# AccessHub

A cloud access-control and alarm platform. Sites (buildings) decide every card swipe locally from their own copy of the access list; the cloud distributes those lists over gap-free, sequenced change streams, ingests what happened, and raises alarms for forced doors, held-open doors, repeated denials and grants made on stale data.

Built on .NET 10 as four microservices with a database each: transactional outbox and inbox on RabbitMQ (MassTransit 8), gRPC streaming to the edge, a saga for the alarm lifecycle, SignalR, Keycloak, OpenTelemetry and Aspire.

| Component                     | What it does                                                                              |
| ----------------------------- | ----------------------------------------------------------------------------------------- |
| `src/Services/Cardholder`     | Sites, doors, cardholders, cards, access groups and schedules.                            |
| `src/Services/AccessControl`  | Decision engine, per-site access lists and change logs, gRPC sync, stale grant detection. |
| `src/Services/EventIngestion` | Idempotent batch endpoint for site events, append-only storage.                           |
| `src/Services/Alarm`          | Alarm rules, lifecycle saga with escalation, SignalR hub.                                 |
| `src/Gateway`                 | YARP entry point: routing, CORS, per-site rate limiting.                                  |
| `src/SiteSimulator`           | Site controllers: local decisions, SQLite buffer, sync with backoff.                      |
| `src/Dashboard`               | React dashboard: live events, alarms, site status.                                        |
| `src/AppHost`                 | Aspire orchestration of everything.                                                       |

## Quick start

Prerequisites: .NET SDK 10.0.101+, Docker, Node.js 22+.

```bash
# Secrets for the local Keycloak realm (user secrets, never committed)
dotnet user-secrets set Parameters:keycloak-admin-password "$(openssl rand -hex 16)" --project src/AppHost
dotnet user-secrets set Parameters:demo-user-password "<choose one>" --project src/AppHost
dotnet user-secrets set Parameters:simulator-client-secret "$(openssl rand -hex 16)" --project src/AppHost

(cd src/Dashboard && npm install)
dotnet run --project src/AppHost --launch-profile http
```

Open the dashboard at http://localhost:5173 and sign in as `admin`, `operator` or `viewer` with the demo password. The simulator registers three sites on first run and starts swiping.

Try it:

- **Revoke a card:** `POST /api/v1/cards/{cardId}/revoke` (as admin). The site denies it within seconds.
- **Simulate an outage:** add `-- --Simulator:OutageSeconds=60` to the run command. Sites keep deciding and upload everything on reconnect.
- **Explain a decision:** `POST /api/v1/access/explain` with `siteId`, `doorId`, `cardNumber`.

## Build and test

```bash
dotnet build -warnaserror
dotnet test                       # needs Docker (Testcontainers)
dotnet format --verify-no-changes
(cd src/Dashboard && npm run lint && npm run build)
k6 run tests/load/ingest-events.js -e GATEWAY_URL=<gateway url> -e ADMIN_PASSWORD=<demo-user-password>
```

## Measured results

Measured 2026-11-12 on one Apple M3 laptop running the whole system through the local AppHost. Details and the full k6 run history: [docs/measured-results.md](docs/measured-results.md).

| Target                                                         | Result                                            |
| -------------------------------------------------------------- | ------------------------------------------------- |
| Revocation reaches a connected site within 5 s                 | 201 ms                                            |
| Forced-door event on the dashboard within 2 s                  | 68 ms (event), 130 ms (alarm)                     |
| Local decision p99 < 1 ms with 10,000 cards                    | Met (asserted by unit test)                       |
| 60 s outage: sites keep deciding, no events lost               | Met                                               |
| RabbitMQ stopped for 70 s: no events lost                      | Met                                               |
| Ingestion 10 × 200 events/s for 2 min, p99 < 250 ms, no errors | p99 241 ms and 71 ms warm, 0 errors (331 ms cold) |

## Deploying

Kubernetes manifests are in `deploy/k8s` (`kubectl apply -k deploy/k8s/overlays/prod`). Build images with `dotnet publish <project> /t:PublishContainer` and create the `accesshub-connections` secret from `deploy/k8s/base/connections.example.env`. Postgres, RabbitMQ (built from `deploy/rabbitmq`), Redis and Keycloak are expected as managed services.
