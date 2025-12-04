using Bunit;

namespace TimeClockUI.Tests;

/// <summary>
/// Basic tests to verify that the test infrastructure is set up correctly
/// </summary>
[TestFixture]
public class SetupVerificationTests
{
    [Test]
    public void BunitContext_CanBeCreated()
    {
        // Arrange & Act
        using var ctx = new Bunit.BunitContext();

        // Assert
        Assert.That(ctx, Is.Not.Null);
        Assert.That(ctx.Services, Is.Not.Null);
    }

    [Test]
    public void BunitFramework_IsWorkingCorrectly()
    {
        // Arrange
        using var ctx = new Bunit.BunitContext();

        // Act - Render a simple HTML fragment to verify bUnit is working
        var cut = ctx.Render(builder => builder.AddMarkupContent(0, "<div>Test</div>"));

        // Assert
        Assert.That(cut, Is.Not.Null);
        Assert.That(cut.Markup, Does.Contain("Test"));
    }
}
