using AccessHub.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Locally one server hosts all four databases (in production each service has its own), so it needs room for every
// service's bounded pool. The argument applies when the persistent container is first created; on an existing
// container use ALTER SYSTEM SET max_connections = 300 and restart it.
var postgres = builder.AddPostgres(ResourceNames.Postgres)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithArgs("-c", "max_connections=300");
var cardholderDb = postgres.AddDatabase(ResourceNames.CardholderDb);
var accessControlDb = postgres.AddDatabase(ResourceNames.AccessControlDb);
var ingestionDb = postgres.AddDatabase(ResourceNames.IngestionDb);
var alarmDb = postgres.AddDatabase(ResourceNames.AlarmDb);

// RabbitMQ is built from deploy/rabbitmq so the delayed message exchange plugin is enabled.
// Persistent with a data volume, so restarting the AppHost never discards messages still queued.
var messaging = builder.AddRabbitMQ(ResourceNames.Messaging)
    .WithDockerfile("../../deploy/rabbitmq")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(targetPort: 15672, name: "management");

var redis = builder.AddRedis(ResourceNames.Redis);

// Keycloak has only a preview Aspire integration, so it runs as a plain container with the realm imported from JSON.
// Its port is fixed because the token issuer URL must be the same for the browser and the services.
var keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);
var demoUserPassword = builder.AddParameter("demo-user-password", secret: true);
var simulatorClientSecret = builder.AddParameter("simulator-client-secret", secret: true);
var keycloak = builder.AddContainer(ResourceNames.Keycloak, "quay.io/keycloak/keycloak", "26.8.2")
    .WithArgs("start-dev", "--import-realm")
    .WithBindMount("./Realms", "/opt/keycloak/data/import", isReadOnly: true)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithEnvironment("DEMO_USER_PASSWORD", demoUserPassword)
    .WithEnvironment("SIMULATOR_CLIENT_SECRET", simulatorClientSecret)
    .WithHttpEndpoint(port: ResourceNames.KeycloakPort, targetPort: 8080, name: "http", isProxied: false)
    .WithHttpEndpoint(targetPort: 9000, name: "management")
    .WithHttpHealthCheck("/health/ready", endpointName: "management");

var keycloakAuthority = $"http://localhost:{ResourceNames.KeycloakPort}/realms/{ResourceNames.Realm}";

var cardholderApi = builder.AddProject<Projects.Cardholder_Api>(ResourceNames.CardholderApi)
    .WithReference(cardholderDb).WaitFor(cardholderDb)
    .WithReference(messaging).WaitFor(messaging)
    .WithEnvironment("Auth__Authority", keycloakAuthority)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

// gRPC needs HTTP/2; Kestrel only serves cleartext HTTP/2 on an HTTP/2-only port, hence the extra "grpc" endpoint.
var accessControlApi = builder.AddProject<Projects.AccessControl_Api>(ResourceNames.AccessControlApi)
    .WithReference(accessControlDb).WaitFor(accessControlDb)
    .WithReference(messaging).WaitFor(messaging)
    .WithEnvironment("Auth__Authority", keycloakAuthority)
    .WaitFor(keycloak)
    .WithHttpEndpoint(name: ResourceNames.GrpcEndpoint, env: "GRPC_PORT")
    .WithHttpHealthCheck("/health/ready");

// One replica locally. Production runs several (deploy/k8s/overlays/prod): each replica's outbox delivery publishes
// different batches, which is how delivery scales past one instance's publisher-confirmed rate. On a single laptop,
// extra replicas only move the CPU contention elsewhere (measured; see README "Measured results").
var ingestionApi = builder.AddProject<Projects.EventIngestion_Api>(ResourceNames.IngestionApi)
    .WithReference(ingestionDb).WaitFor(ingestionDb)
    .WithReference(messaging).WaitFor(messaging)
    .WithEnvironment("Auth__Authority", keycloakAuthority)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

var alarmApi = builder.AddProject<Projects.Alarm_Api>(ResourceNames.AlarmApi)
    .WithReference(alarmDb).WaitFor(alarmDb)
    .WithReference(messaging).WaitFor(messaging)
    .WithReference(redis).WaitFor(redis)
    .WithEnvironment("Auth__Authority", keycloakAuthority)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

var gateway = builder.AddProject<Projects.Gateway>(ResourceNames.Gateway)
    .WithReference(cardholderApi)
    .WithReference(accessControlApi)
    .WithReference(ingestionApi)
    .WithReference(alarmApi)
    .WithHttpEndpoint(name: ResourceNames.GrpcEndpoint, env: "GRPC_PORT")
    .WithExternalHttpEndpoints();

// The dashboard runs on a fixed port because the Keycloak client's redirect URI is registered for it.
builder.AddViteApp(ResourceNames.Dashboard, "../Dashboard")
    .WithEndpoint("http", e => e.Port = ResourceNames.DashboardPort)
    .WithEnvironment("VITE_GATEWAY_URL", gateway.GetEndpoint("http"))
    .WithEnvironment("VITE_OIDC_AUTHORITY", keycloakAuthority)
    .WithEnvironment("VITE_OIDC_CLIENT_ID", "dashboard")
    .WaitFor(gateway);

// Site controllers reach the cloud only through the gateway. Pass --Simulator:OutageSeconds=60 to the AppHost to
// demonstrate an outage.
var simulator = builder.AddProject<Projects.SiteSimulator>(ResourceNames.Simulator)
    .WithReference(gateway)
    .WaitFor(gateway).WaitFor(cardholderApi).WaitFor(accessControlApi).WaitFor(ingestionApi)
    .WithEnvironment("Simulator__TokenEndpoint", $"{keycloakAuthority}/protocol/openid-connect/token")
    .WithEnvironment("Simulator__ClientSecret", simulatorClientSecret)
    .WithEnvironment("Simulator__DataDirectory", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "simulator-data")))
    .WithArgs("--sites", "3", "--doors", "4");
if (builder.Configuration["Simulator:OutageSeconds"] is { Length: > 0 } outageSeconds)
{
    simulator.WithArgs("--simulate-outage", outageSeconds);
}


await builder.Build().RunAsync();
