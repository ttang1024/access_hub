namespace AccessHub.AppHost;

/// <summary>Resource and connection names shared between the AppHost and the services' configuration.</summary>
internal static class ResourceNames
{
    public const string Postgres = "postgres";
    public const string CardholderDb = "cardholderdb";
    public const string AccessControlDb = "accesscontroldb";
    public const string IngestionDb = "ingestiondb";
    public const string AlarmDb = "alarmdb";
    public const string Messaging = "messaging";
    public const string Redis = "redis";
    public const string Keycloak = "keycloak";
    public const string Realm = "accesshub";
    public const string CardholderApi = "cardholder-api";
    public const string AccessControlApi = "access-control-api";
    public const string IngestionApi = "event-ingestion-api";
    public const string AlarmApi = "alarm-api";
    public const string Gateway = "gateway";
    public const string Dashboard = "dashboard";
    public const string Simulator = "site-simulator";
    public const string GrpcEndpoint = "grpc";
    public const int KeycloakPort = 8080;
    public const int DashboardPort = 5173;
}
