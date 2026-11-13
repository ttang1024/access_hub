# ADR 0009: MassTransit pinned to 8.x

- Status: Accepted
- Date: 2026-11-12

## Context

MassTransit provides the outbox and inbox, saga persistence, scheduling, retries and the test harness this system relies on. Version 9 is distributed under a commercial licence; 8.x is Apache 2.0.

## Decision

Pin `MassTransit`, `MassTransit.RabbitMQ` and `MassTransit.EntityFrameworkCore` to the latest 8.x release (8.5.12 at the time of writing) in `Directory.Packages.props`. Do not upgrade to 9.x without a licensing decision.

## Consequences

- 8.x receives security fixes for a limited period. When that ends the options are a commercial licence, a fork, or replacing MassTransit.
- Replacement cost is contained but not small: the transport-facing code is concentrated in `BuildingBlocks.Infrastructure.Messaging`, the consumers and the alarm saga. The saga is the expensive part ([ADR 0008](0008-alarm-lifecycle-as-saga.md)).
- Automated dependency updates must ignore 9.x. None is configured in this repository yet.
- Features introduced only in 9.x are unavailable.
