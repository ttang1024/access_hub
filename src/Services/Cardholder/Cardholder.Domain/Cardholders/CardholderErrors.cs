using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.Cardholders;

public static class CardholderErrors
{
    public static Error NotFound(CardholderId id) => Error.NotFound("Cardholder.NotFound", $"Cardholder {id} was not found.");

    public static Error InvalidName => Error.Validation("Cardholder.InvalidName", "First and last name are required and at most 100 characters each.");

    public static Error VersionMismatch(int expected, int actual) =>
        Error.Conflict("Cardholder.VersionMismatch", $"Expected version {expected} but the cardholder is at version {actual}.");

    public static Error TooManyGroups => Error.Validation("Cardholder.TooManyGroups", "A cardholder can belong to at most 50 access groups.");
}
