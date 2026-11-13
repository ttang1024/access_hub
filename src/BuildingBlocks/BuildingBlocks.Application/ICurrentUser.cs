namespace AccessHub.BuildingBlocks.Application;

/// <summary>The authenticated caller, used for auditing and authorisation decisions inside handlers.</summary>
public interface ICurrentUser
{
    /// <summary>Stable user identifier (JWT <c>sub</c>), or a system identity for background work.</summary>
    string UserId { get; }

    /// <summary>Display name for audit entries.</summary>
    string UserName { get; }
}
