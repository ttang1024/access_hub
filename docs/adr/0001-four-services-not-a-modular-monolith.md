# ADR 0001: Four services, not a modular monolith

- Status: Accepted
- Date: 2026-11-12

## Context

AccessHub has four areas of behaviour: managing who may open which door (Cardholder), distributing that to sites and holding the decision rules (AccessControl), receiving what happened at sites (EventIngestion), and reacting to it (Alarm).

A modular monolith with the same module boundaries would be simpler to build, deploy and debug. The split has to be justified by real differences between the areas.

## Decision

Run them as four separately deployable services that communicate only through RabbitMQ, because their profiles differ:

| | Load | Availability need | Change profile |
| --- | --- | --- | --- |
| Cardholder | Low, operator-driven CRUD | Can be down for minutes: sites keep their lists and AccessControl keeps serving them | Changes with business rules for people and groups |
| AccessControl | One long-lived gRPC stream per site, plus fan-out | High: it feeds every site | Rarely; the decision rules are shared with the edge and must stay stable |
| EventIngestion | Highest and spikiest writes (sites drain buffers after outages) | High: sites buffer, but a long outage fills their disks | Rarely; append-only |
| Alarm | Moderate, event-driven; holds WebSocket connections to dashboards | Medium: nothing is lost while it is down, it catches up from the queue | Often (new rules, workflow) |

What the split buys:

- EventIngestion scales on its own (3 to 10 replicas behind an HPA in the prod overlay).
- An Alarm deploy cannot touch the path that feeds sites their access lists.
- "AccessControl keeps serving when Cardholder is down" is a deployment fact, not a coding discipline.

## Alternative rejected

**Modular monolith.** One process means one failure domain and one scaling unit: an ingestion spike would compete with the sync streams for the same CPU and connection pool, and every Alarm rule change would restart the process holding every site's stream.

## Consequences

- Cost: four databases, an outbox and inbox per service, eventual consistency, versioned integration events. Local development is harder (mitigated by the Aspire AppHost), as is debugging (mitigated by end-to-end traces).
- Cross-service invariants, such as "a door's site must exist", are enforced where the data lives (Cardholder). Downstream services tolerate events in any order.
- The decision is reversible. The services could merge into a modular monolith along the same boundaries without changing domain code; the messaging contracts would become in-process.
