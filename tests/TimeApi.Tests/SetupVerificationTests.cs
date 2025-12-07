using NUnit.Framework;
using FluentAssertions;

namespace TimeApi.Tests;

/// <summary>
/// Simple tests to verify that the test infrastructure is set up correctly.
/// </summary>
[TestFixture]
public class SetupVerificationTests
{
    [Test]
    public void TestInfrastructure_IsConfiguredCorrectly()
    {
        // Arrange & Act
        var result = true;

        // Assert
        result.Should().BeTrue("test infrastructure should be set up correctly");
    }

    [Test]
    public void FluentAssertions_WorksCorrectly()
    {
        // Arrange
        var expected = "Hello, World!";
        var actual = "Hello, World!";

        // Assert
        actual.Should().Be(expected);
        actual.Should().NotBeNullOrEmpty();
        actual.Should().StartWith("Hello");
    }
}
