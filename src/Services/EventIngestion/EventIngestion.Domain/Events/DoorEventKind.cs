namespace AccessHub.EventIngestion.Domain.Events;

public enum DoorEventKind
{
    AccessGranted = 1,
    AccessDenied = 2,
    DoorOpened = 3,
    DoorClosed = 4,
    DoorForced = 5,
    DoorHeldOpen = 6,
}
