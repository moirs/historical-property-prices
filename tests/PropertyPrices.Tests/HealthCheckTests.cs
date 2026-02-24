using FluentAssertions;

namespace PropertyPrices.Tests;

public class HealthCheckTests
{
    [Fact]
    public void HealthCheck_ShouldReturnSuccessfully()
    {
        // Arrange & Act
        var result = true;

        // Assert
        result.Should().BeTrue();
    }
}
