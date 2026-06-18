using Dashboard_v2.Application.Common.Exceptions;
using Dashboard_v2.Application.Common.Models;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Common;

[TestFixture]
public class ForbiddenAccessExceptionTests
{
    [Test]
    public void DefaultConstructor_CreatesException()
    {
        var ex = new ForbiddenAccessException();
        ex.ShouldNotBeNull();
        ex.ShouldBeOfType<ForbiddenAccessException>();
    }

    [Test]
    public void IsException()
    {
        var ex = new ForbiddenAccessException();
        (ex is Exception).ShouldBeTrue();
    }
}

[TestFixture]
public class ResultTests
{
    [Test]
    public void Success_HasSucceededTrue_AndNoErrors()
    {
        var result = Result.Success();
        result.Succeeded.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Test]
    public void Failure_HasSucceededFalse_AndErrors()
    {
        var result = Result.Failure(["Error 1", "Error 2"]);
        result.Succeeded.ShouldBeFalse();
        result.Errors.Length.ShouldBe(2);
        result.Errors.ShouldContain("Error 1");
    }
}
