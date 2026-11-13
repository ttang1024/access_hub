using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.Doors;

public static class DoorErrors
{
    public static Error NotFound(DoorId id) => Error.NotFound("Door.NotFound", $"Door {id} was not found.");

    public static Error InvalidName => Error.Validation("Door.InvalidName", "Door name is required and at most 200 characters.");
}
