namespace AccessHub.AccessControl.Application.Sync;

public sealed class SyncOptions
{
    public const string Section = "Sync";

    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);

    public int SnapshotChunkSize { get; set; } = 500;

    public int ReadBatchSize { get; set; } = 500;
}
