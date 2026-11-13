namespace AccessHub.BuildingBlocks.Domain;

/// <summary>Classifies an expected failure so the API layer can choose the HTTP or gRPC status.</summary>
public enum ErrorType
{
    /// <summary>Input failed validation.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The request conflicts with the current state.</summary>
    Conflict,

    /// <summary>The caller is not allowed to perform the operation.</summary>
    Forbidden,

    /// <summary>A domain rule rejected the request.</summary>
    Failure,
}
