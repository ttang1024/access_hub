using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.BuildingBlocks.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithValue_ExposesValue()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_ReadingValue_Throws()
    {
        Result<int> result = Error.NotFound("Thing.NotFound", "missing");

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Error_SameContent_IsEqual()
    {
        var a = Error.Validation(new Dictionary<string, string[]> { ["Name"] = ["required"] });
        var b = Error.Validation(new Dictionary<string, string[]> { ["Name"] = ["required"] });

        a.ShouldBe(b);
        Error.NotFound("X", "y").ShouldBe(Error.NotFound("X", "y"));
        Error.NotFound("X", "y").ShouldNotBe(Error.Conflict("X", "y"));
    }
}
