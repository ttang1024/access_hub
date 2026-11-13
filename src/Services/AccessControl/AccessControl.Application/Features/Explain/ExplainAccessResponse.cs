namespace AccessHub.AccessControl.Application.Features.Explain;

public sealed record ExplainAccessResponse(bool Granted, string? DenyReason, DateTimeOffset EvaluatedAt, long AccessListSequence);
