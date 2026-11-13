namespace AccessHub.AccessControl.Application.Abstractions;

public sealed record ChangeLogPosition(long LastSequence, long PrunedThroughSequence);
