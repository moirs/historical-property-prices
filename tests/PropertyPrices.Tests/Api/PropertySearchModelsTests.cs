using FluentAssertions;
using PropertyPrices.Api.Models;
using Xunit;

namespace PropertyPrices.Tests.Api;

public class PropertySearchRequestTests
{
    [Fact]
    public void Validate_WithValidRequest_ReturnsNoErrors()
    {
        // Arrange
        var request = new PropertySearchRequest
        {
            Postcode = "SW1A 1AA",
            DateFrom = new DateOnly(2023, 01, 01),
            DateTo = new DateOnly(2023, 12, 31),
            PriceMin = 100000,
            PriceMax = 1000000,
            PageNumber = 1,
            PageSize = 50
        };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithDefaultValues_ReturnsNoErrors()
    {
        // Arrange
        var request = new PropertySearchRequest();

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvertedDateRange_ReturnsError()
    {
        // Arrange
        var request = new PropertySearchRequest
        {
            DateFrom = new DateOnly(2023, 12, 31),
            DateTo = new DateOnly(2023, 01, 01)
        };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().ContainMatch("*DateFrom*less than or equal to*DateTo*");
    }

    [Fact]
    public void Validate_WithInvertedPriceRange_ReturnsError()
    {
        // Arrange
        var request = new PropertySearchRequest
        {
            PriceMin = 1000000,
            PriceMax = 100000
        };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().ContainMatch("*PriceMin*less than or equal to*PriceMax*");
    }

    [Fact]
    public void Validate_WithNegativePrice_ReturnsError()
    {
        // Arrange
        var request = new PropertySearchRequest { PriceMin = -1000 };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().ContainMatch("*PriceMin*non-negative*");
    }

    [Fact]
    public void Validate_WithInvalidPageNumber_ReturnsError()
    {
        // Arrange
        var request = new PropertySearchRequest { PageNumber = 0 };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().ContainMatch("*PageNumber*>= 1*");
    }

    [Fact]
    public void Validate_WithPageSizeTooSmall_ReturnsError()
    {
        // Arrange
        var request = new PropertySearchRequest { PageSize = 0 };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().ContainMatch("*PageSize*between 1 and 1000*");
    }

    [Fact]
    public void Validate_WithPageSizeTooLarge_ReturnsError()
    {
        // Arrange
        var request = new PropertySearchRequest { PageSize = 1001 };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().ContainMatch("*PageSize*between 1 and 1000*");
    }

    [Fact]
    public void Validate_WithMaxPageSize_ReturnsNoErrors()
    {
        // Arrange
        var request = new PropertySearchRequest { PageSize = 1000 };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new PropertySearchRequest
        {
            DateFrom = new DateOnly(2023, 12, 31),
            DateTo = new DateOnly(2023, 01, 01),
            PriceMin = 1000000,
            PriceMax = 100000,
            PageNumber = -1,
            PageSize = 2000
        };

        // Act
        var errors = request.Validate();

        // Assert
        errors.Should().HaveCount(4);
        errors.Should().ContainMatch("*DateFrom*");
        errors.Should().ContainMatch("*PriceMin*");
        errors.Should().ContainMatch("*PageNumber*");
        errors.Should().ContainMatch("*PageSize*");
    }

    [Fact]
    public void PropertySearchResponse_WithData_CalculatesPaginationCorrectly()
    {
        // Arrange
        var response = new PropertySearchResponse
        {
            Results = new(),
            TotalCount = 150,
            PageNumber = 1,
            PageSize = 50
        };

        // Act & Assert
        response.TotalPages.Should().Be(3);
        response.HasNextPage.Should().BeTrue();
        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PropertySearchResponse_OnLastPage_ReturnsCorrectPaginationFlags()
    {
        // Arrange
        var response = new PropertySearchResponse
        {
            Results = new(),
            TotalCount = 150,
            PageNumber = 3,
            PageSize = 50
        };

        // Act & Assert
        response.TotalPages.Should().Be(3);
        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PropertySearchResponse_WithOddTotalCount_CalculatesTotalPagesCorrectly()
    {
        // Arrange & Act & Assert
        var response = new PropertySearchResponse
        {
            TotalCount = 101,
            PageSize = 50,
            PageNumber = 1
        };
        response.TotalPages.Should().Be(3); // ceil(101/50) = 3
    }
}

public class PropertySearchResponseTests
{
    [Fact]
    public void PropertySearchResponse_WithSingleResult_SetsPaginationCorrectly()
    {
        // Arrange
        var response = new PropertySearchResponse
        {
            Results = new() { new PropertyDto { Address = "Test" } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 50
        };

        // Act & Assert
        response.TotalPages.Should().Be(1);
        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeFalse();
    }
}
