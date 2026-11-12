using FluentAssertions;
using Xunit;

namespace D2ApiCreator.Tests.Unit.Domain;

/// <summary>
/// Example unit test for Value Objects
/// </summary>
public class ValueObjectTests
{
    [Fact]
    public void ValueObjects_WithSameValues_ShouldBeEqual()
    {
        // This is an example test structure
        // You'll add your actual value object tests here
        var result = true;

        result.Should().BeTrue();
    }
}

