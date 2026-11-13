using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.Cards;

/// <summary>The number a reader sees: 4 to 32 characters, upper-case letters and digits.</summary>
public sealed record CardNumber : IValueObject
{
    public const int MinLength = 4;
    public const int MaxLength = 32;

    private CardNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<CardNumber> Create(string? value)
    {
        var normalised = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalised) || normalised.Length is < MinLength or > MaxLength || !normalised.All(char.IsAsciiLetterOrDigit))
        {
            return CardErrors.InvalidNumber;
        }

        return new CardNumber(normalised);
    }

    public override string ToString() => Value;
}
