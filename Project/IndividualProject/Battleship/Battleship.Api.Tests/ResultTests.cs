namespace Battleship.Api.Tests;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void ImplicitFromValue_IsASuccessCarryingThatValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ImplicitFromError_IsAFailureWithoutAValue()
    {
        Result<int> result = Error.Conflict("nope");

        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Conflict, "nope"), result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Map_OnSuccess_TransformsTheValue()
    {
        Result<int> result = 2;

        Assert.Equal("2!", result.Map(value => $"{value}!").Value);
    }

    [Fact]
    public void Map_OnFailure_KeepsTheErrorAndSkipsTheFunction()
    {
        Result<int> result = Error.NotFound("gone");
        var called = false;

        var mapped = result.Map(value =>
        {
            called = true;
            return value;
        });

        Assert.False(called);
        Assert.Equal(ErrorType.NotFound, mapped.Error.Type);
    }

    [Fact]
    public void Bind_OnSuccess_ReturnsTheNextResult()
    {
        Result<int> result = 2;

        var next = result.Bind<string>(value => value > 1 ? "big" : Error.Validation("small"));

        Assert.Equal("big", next.Value);
    }

    [Fact]
    public void Bind_OnFailure_KeepsTheFirstErrorAndSkipsTheFunction()
    {
        Result<int> result = Error.Unauthorized("who");
        var called = false;

        var next = result.Bind<string>(_ =>
        {
            called = true;
            return "never";
        });

        Assert.False(called);
        Assert.Equal(ErrorType.Unauthorized, next.Error.Type);
    }
}
