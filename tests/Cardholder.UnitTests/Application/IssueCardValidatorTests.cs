using AccessHub.Cardholder.Application.Features.Cards;
using Microsoft.Extensions.Time.Testing;

namespace AccessHub.Cardholder.UnitTests.Application;

public sealed class IssueCardValidatorTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Validate_PastExpiry_FailsOnExpiresAt()
    {
        var result = new IssueCardValidator(_clock).Validate(new IssueCardCommand(Guid.NewGuid(), "1234", _clock.GetUtcNow().AddMinutes(-1)));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(IssueCardCommand.ExpiresAt));
    }

    [Fact]
    public void Validate_EmptyCardholder_FailsOnCardholderId()
    {
        var result = new IssueCardValidator(_clock).Validate(new IssueCardCommand(Guid.Empty, "1234", null));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(IssueCardCommand.CardholderId));
    }
}
