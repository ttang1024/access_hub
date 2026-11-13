namespace AccessHub.ArchitectureTests;

/// <summary>Assembly-name prefixes that inner layers must not reference.</summary>
internal static class ForbiddenDependencies
{
    /// <summary>Infrastructure packages a Domain project must never reference.</summary>
    public static readonly string[] ForDomain =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "MassTransit",
        "Npgsql",
        "Grpc",
        "Google.Protobuf",
        "FluentValidation",
        "StackExchange.Redis",
        "Microsoft.Data.Sqlite",
    ];

    /// <summary>Infrastructure packages an Application project must never reference.</summary>
    public static readonly string[] ForApplication =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "MassTransit",
        "Npgsql",
        "Grpc",
        "StackExchange.Redis",
    ];
}
