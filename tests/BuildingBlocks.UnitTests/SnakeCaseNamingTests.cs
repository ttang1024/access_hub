using AccessHub.BuildingBlocks.Infrastructure.Persistence;

namespace AccessHub.BuildingBlocks.UnitTests;

public sealed class SnakeCaseNamingTests
{
    [Theory]
    [InlineData("CardNumber", "card_number")]
    [InlineData("SiteId", "site_id")]
    [InlineData("already_snake", "already_snake")]
    [InlineData("PKCardholders", "pk_cardholders")]
    [InlineData("IX_Cards_Number", "ix_cards_number")]
    public void ToSnakeCase_Name_ConvertsToSnakeCase(string input, string expected) =>
        SnakeCaseNaming.ToSnakeCase(input).ShouldBe(expected);
}
