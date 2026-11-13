# ADR 0008: Alarm lifecycle as a saga

- Status: Accepted
- Date: 2026-11-12

## Context

An alarm moves Raised → Acknowledged → Closed. A high-priority alarm escalates if it is not acknowledged within 2 minutes, and acknowledging cancels that. Further triggers for the same subject must add occurrences to the open alarm, not raise new ones.

Sagas usually coordinate work across services. This lifecycle lives entirely inside Alarm, so a plain aggregate with a repository would also work.

## Decision

Model the lifecycle as a MassTransit state machine saga, persisted with the EF Core saga repository (pessimistic locking, Postgres). The reasons are about time and messages, not distribution:

- **Timeouts are built in.** Escalation is a scheduled message (RabbitMQ delayed exchange) owned by the saga, with its token stored on the instance. Acknowledging unschedules it.
- **Correlation is built in.** A trigger correlates to the open alarm for its subject (`subject_key` and not Closed) or starts a new instance. A partial unique index makes a concurrent double start fail and retry onto the existing alarm, so the one-open-alarm rule is enforced by the database.
- **Every transition is a message handled with the inbox and outbox.** Retries and redeliveries cannot double-acknowledge or double-publish, and `AlarmRaised`, `AlarmEscalated`, `AlarmAcknowledged` and `AlarmClosed` commit with the state change.
- **Invalid transitions are explicit.** The API sends commands by request/response. The saga answers Accepted or Rejected (mapped to 409) and changes nothing on rejection.

The pure rules stay in `Alarm.Domain`: which events raise which alarm, and the 60-second repeated-denials window. Only the lifecycle is in the saga.

## Alternative rejected

**A plain `Alarm` aggregate.** Simpler for the state transitions themselves, but escalation would need a hand-built scheduler table, a poller and cancellation logic, and the trigger consumers would need their own concurrency handling to keep one open alarm per subject. That is a saga written by hand.

## Consequences

- The state machine DSL is MassTransit-specific, so the saga lives in Infrastructure, not Domain.
- Acknowledge and close go through the broker. That adds a few milliseconds and makes operator actions depend on RabbitMQ being up.
- Escalation depends on the delayed exchange plugin (`deploy/rabbitmq`).
- The saga locks one row per alarm. Under an alarm storm, with thousands of subjects triggering at once, it is lock-bound; this was observed in load testing and is not covered by a performance target.
- Every transition is covered by harness tests, including escalation and its cancellation.
