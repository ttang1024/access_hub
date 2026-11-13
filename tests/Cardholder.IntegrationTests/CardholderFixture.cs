using AccessHub.Cardholder.Infrastructure;
using AccessHub.Cardholder.Infrastructure.Persistence;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using AccessHub.Testing;

[assembly: AssemblyFixture(typeof(AccessHub.Cardholder.IntegrationTests.CardholderFixture))]

namespace AccessHub.Cardholder.IntegrationTests;

public sealed class CardholderFixture : ServiceFixture<Program, CardholderDbContext>
{
    protected override string DatabaseConnectionName => CardholderInfrastructureRegistration.ConnectionName;

    protected override IEnumerable<Type> ListenedEventTypes =>
    [
        typeof(SiteRegistered), typeof(SiteApiKeyRotated), typeof(DoorRegistered),
        typeof(CardholderChanged), typeof(CardIssued), typeof(CardRevoked), typeof(AccessGroupChanged),
    ];
}
