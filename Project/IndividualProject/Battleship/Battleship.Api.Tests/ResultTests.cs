namespace Battleship.Api.Tests;

public class ResultTests
{
    [Test]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsFailure, Is.False);
        Assert.That(() => result.Error, Throws.InvalidOperationException);
    }

    [Test]
    public void ImplicitFromValue_IsASuccessCarryingThatValue()
    {
        Result<int> result = 42;

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void ImplicitFromError_IsAFailureWithoutAValue()
    {
        Result<int> result = Error.Conflict("nope");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(new Error(ErrorType.Conflict, "nope")));
        Assert.That(() => result.Value, Throws.InvalidOperationException);
    }

    [Test]
    public void Map_OnSuccess_TransformsTheValue()
    {
        Result<int> result = 2;

        Assert.That(result.Map(value => $"{value}!").Value, Is.EqualTo("2!"));
    }

    [Test]
    public void Map_OnFailure_KeepsTheErrorAndSkipsTheFunction()
    {
        Result<int> result = Error.NotFound("gone");
        var called = false;

        var mapped = result.Map(value =>
        {
            called = true;
            return value;
        });

        Assert.That(called, Is.False);
        Assert.That(mapped.Error.Type, Is.EqualTo(ErrorType.NotFound));
    }

    [Test]
    public void Bind_OnSuccess_ReturnsTheNextResult()
    {
        Result<int> result = 2;

        var next = result.Bind<string>(value => value > 1 ? "big" : Error.Validation("small"));

        Assert.That(next.Value, Is.EqualTo("big"));
    }

    [Test]
    public void Bind_OnFailure_KeepsTheFirstErrorAndSkipsTheFunction()
    {
        Result<int> result = Error.Unauthorized("who");
        var called = false;

        var next = result.Bind<string>(_ =>
        {
            called = true;
            return "never";
        });

        Assert.That(called, Is.False);
        Assert.That(next.Error.Type, Is.EqualTo(ErrorType.Unauthorized));
    }
}
