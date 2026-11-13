namespace AccessHub.Cardholder.Application.Abstractions;

public sealed record CardholderDto(Guid Id, string FirstName, string LastName, bool IsActive, IReadOnlyList<Guid> AccessGroupIds, int Version);
