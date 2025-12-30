using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Tests.Core;

public class ResultTests
{
    [Fact]
    public void Ok_CreatesSuccessfulResult()
    {
        var result = Result<string>.Ok("test value");

        Assert.True(result.Success);
        Assert.Equal("test value", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_CreatesFailedResult()
    {
        var result = Result<string>.Fail("error message");

        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal("error message", result.Error);
    }

    [Fact]
    public void Fail_WithSuggestions_IncludesSuggestions()
    {
        var suggestions = new List<string> { "Try this", "Or try that" };
        var result = Result<string>.Fail("error", suggestions: suggestions);

        Assert.False(result.Success);
        Assert.Equal("error", result.Error);
        Assert.NotNull(result.Suggestions);
        Assert.Equal(2, result.Suggestions.Count);
        Assert.Contains("Try this", result.Suggestions);
    }

    [Fact]
    public void ToJson_SuccessResult_ContainsSuccessTrue()
    {
        var result = Result<string>.Ok("value");
        var json = result.ToJson(true);

        Assert.Contains("\"success\": true", json);
        Assert.Contains("\"value\"", json);
    }

    [Fact]
    public void ToJson_FailedResult_ContainsSuccessFalse()
    {
        var result = Result<string>.Fail("something went wrong");
        var json = result.ToJson(true);

        Assert.Contains("\"success\": false", json);
        Assert.Contains("\"error\"", json);
        Assert.Contains("something went wrong", json);
    }

    [Fact]
    public void NonGenericResult_Ok_IsSuccessful()
    {
        var result = Result.Ok();

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void NonGenericResult_Fail_HasError()
    {
        var result = Result.Fail("failed");

        Assert.False(result.Success);
        Assert.Equal("failed", result.Error);
    }
}
