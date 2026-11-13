namespace AccessHub.Contracts.IntegrationEvents.EventIngestion;

/// <summary>Kinds of event a site controller reports.</summary>
public enum DoorEventType
{
    /// <summary>A card was granted access.</summary>
    AccessGranted = 1,

    /// <summary>A card was denied access.</summary>
    AccessDenied = 2,

    /// <summary>The door opened.</summary>
    DoorOpened = 3,

    /// <summary>The door closed.</summary>
    DoorClosed = 4,

    /// <summary>The door opened without a grant.</summary>
    DoorForced = 5,

    /// <summary>The door stayed open too long.</summary>
    DoorHeldOpen = 6,
}
