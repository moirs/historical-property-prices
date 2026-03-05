using FluentAssertions;
using PropertyPrices.Core.Sparql;
using PropertyPrices.Core.Sparql.Models;
using Xunit;

namespace PropertyPrices.Tests.Sparql;

public class SparqlQueryBuilderTests
{
    [Fact]
    public void Build_WithNoFilters_ReturnsValidSparqlQuery()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();

        // Act
        var query = builder.Build();

        // Assert
        query.Should().NotBeNullOrEmpty();
        query.Should().Contain("SELECT ?paon ?saon ?street ?town ?county ?postcode ?amount ?date ?category");
        query.Should().Contain("WHERE {");
        query.Should().Contain("lrcommon:postcode");
        query.Should().Contain("lrppi:propertyAddress");
        query.Should().Contain("ORDER BY ?amount");
    }

    [Fact]
    public void Build_WithPostcode_IncludesPostcodeFilter()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act
        var query = builder
            .WithPostcode("SW1A 1AA")
            .Build();

        // Assert
        query.Should().Contain("SW1A 1AA"); // Should be normalized (uppercase, space preserved)
        query.Should().Contain("VALUES ?postcode {\"SW1A 1AA\"^^xsd:string}");
    }

    [Fact]
    public void Build_WithAddressContains_IncludesAddressFilter()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act
        var query = builder
            .WithAddressContains("Baker Street")
            .Build();

        // Assert
        query.Should().Contain("FILTER(regex(concat(str(?paon)");
        query.Should().Contain("Baker Street");
    }

    [Fact]
    public void Build_WithDateRange_IncludesdateFilters()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        var startDate = new DateOnly(2020, 1, 1);
        var endDate = new DateOnly(2023, 12, 31);
        
        // Act
        var query = builder
            .WithDateRange(startDate, endDate)
            .Build();

        // Assert
        query.Should().Contain("FILTER(?date >= \"2020-01-01\"^^xsd:date)");
        query.Should().Contain("FILTER(?date <= \"2023-12-31\"^^xsd:date)");
    }

    [Fact]
    public void Build_WithPropertyType_IncludesPropertyTypeFilter()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act
        var query = builder
            .WithPropertyType("T")
            .Build();

        // Assert
        query.Should().Contain("SELECT ?paon ?saon ?street ?town ?county ?postcode ?amount ?date ?category");
        query.Should().Contain("FILTER(!BOUND(?propertyType)");
    }

    [Theory]
    [InlineData("D")]
    [InlineData("S")]
    [InlineData("T")]
    [InlineData("F")]
    [InlineData("O")]
    public void Build_WithVariousPropertyTypes_MapsCorrectly(string propertyTypeCode)
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act
        var query = builder
            .WithPropertyType(propertyTypeCode)
            .Build();

        // Assert
        query.Should().Contain("SELECT ?paon ?saon ?street ?town ?county ?postcode ?amount ?date ?category");
        query.Should().Contain("FILTER(!BOUND(?propertyType)");
    }

    [Fact]
    public void Build_WithPagination_IncludesLimitAndOffset()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act
        var query = builder
            .WithPagination(limit: 100, offset: 50)
            .Build();

        // Assert
        query.Should().Contain("LIMIT 100");
        query.Should().Contain("OFFSET 50");
    }

    [Fact]
    public void Build_WithPaginationNoOffset_IncludesOnlyLimit()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act
        var query = builder
            .WithPagination(limit: 50)
            .Build();

        // Assert
        query.Should().Contain("LIMIT 50");
        query.Should().NotContain("OFFSET");
    }

    [Fact]
    public void Build_WithMultipleFilters_CombinesAllConstraints()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        var startDate = new DateOnly(2022, 1, 1);
        var endDate = new DateOnly(2022, 12, 31);
        
         // Act
         var query = builder
             .WithPostcode("M1 1AA")
             .WithPropertyType("D")
             .WithDateRange(startDate, endDate)
             .WithPagination(limit: 100)
             .Build();

        // Assert
        query.Should().Contain("M1 1AA");
        query.Should().Contain("2022-01-01");
        query.Should().Contain("2022-12-31");
        query.Should().Contain("LIMIT 100");
        query.Should().Contain("VALUES ?postcode");
    }

    [Fact]
    public void ToString_ReturnsSameAsBuilt()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act
        var toStringResult = builder
            .WithPostcode("SW1A 1AA")
            .ToString();
        var buildResult = new SparqlQueryBuilder()
            .WithPostcode("SW1A 1AA")
            .Build();

        // Assert
        toStringResult.Should().Be(buildResult);
    }

    [Fact]
    public void WithPostcode_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.WithPostcode("TOOLONGPOSTCODE"));
        
        ex.ParamName.Should().Be("postcode");
        ex.Message.Should().Contain("5-8 characters");
    }

    [Fact]
    public void WithPostcode_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.WithPostcode(""));
        
        ex.ParamName.Should().Be("postcode");
    }

    [Fact]
    public void WithAddressContains_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.WithAddressContains(""));
        
        ex.ParamName.Should().Be("addressSubstring");
    }

    [Fact]
    public void WithDateRange_StartAfterEnd_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        var startDate = new DateOnly(2023, 12, 31);
        var endDate = new DateOnly(2023, 1, 1);
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.WithDateRange(startDate, endDate));
        
        ex.ParamName.Should().Be("startDate");
        ex.Message.Should().Contain("cannot be after");
    }

    [Fact]
    public void WithDateRange_BeforePricePaidStart_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        var startDate = new DateOnly(1994, 12, 31); // Before 1995
        var endDate = new DateOnly(2020, 1, 1);
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.WithDateRange(startDate, endDate));
        
        ex.ParamName.Should().Be("startDate");
        ex.Message.Should().Contain("1995");
    }

    [Fact]
    public void WithPagination_InvalidLimit_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.WithPagination(limit: 0));
        
        ex.ParamName.Should().Be("limit");
        ex.Message.Should().Contain("greater than 0");
    }

    [Fact]
    public void WithPagination_NegativeLimit_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.WithPagination(limit: -1));
        
        ex.ParamName.Should().Be("limit");
    }

    [Fact]
    public void WithPagination_NegativeOffset_ThrowsArgumentException()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            builder.WithPagination(limit: 10, offset: -1));
        
        ex.ParamName.Should().Be("offset");
        ex.Message.Should().Contain("0 or greater");
    }

    [Fact]
    public void FluentChaining_AllowsSequentialCalls()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act - Verify no exceptions are thrown and chain works
        var query = builder
            .WithPostcode("SW1A 1AA")
            .WithAddressContains("Street")
            .WithDateRange(new DateOnly(2020, 1, 1), new DateOnly(2023, 12, 31))
            .WithPropertyType("F")
            .WithPagination(10, 5)
            .Build();

        // Assert
        query.Should().NotBeNullOrEmpty();
        query.Should().Contain("SW1A1AA");
        query.Should().Contain("Street");
        query.Should().Contain("flat");  // lowercase in FILTER
    }

    [Fact]
    public void WithPostcode_NormalizesFOrmatAndCase()
    {
        // Arrange
        var builder = new SparqlQueryBuilder();
        
        // Act
        var query = builder
            .WithPostcode("m1 1aa") // lowercase with space
            .Build();

        // Assert
        query.Should().Contain("M1 1AA"); // Should be uppercase and space preserved
    }
}
