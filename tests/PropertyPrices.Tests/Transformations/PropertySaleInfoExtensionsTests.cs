using FluentAssertions;
using PropertyPrices.Core.Models;
using PropertyPrices.Core.Transformations;
using Xunit;

namespace PropertyPrices.Tests.Transformations;

public class PropertySaleInfoExtensionsTests
{
    private List<PropertySaleInfo> CreateTestData()
    {
        return new()
        {
            new PropertySaleInfo(
                new Address("10 Downing Street", null, "SW1A 1AA", "SW1A"),
                1500000,
                new DateOnly(2023, 06, 15)),
            new PropertySaleInfo(
                new Address("42 Bankside", null, "SE1 7AA", "SE1"),
                450000,
                new DateOnly(2023, 05, 20)),
            new PropertySaleInfo(
                new Address("250 Bow Street", null, "WC2E 8RD", "WC2E"),
                850000,
                new DateOnly(2023, 04, 10)),
            new PropertySaleInfo(
                new Address("1 Oxford Street", null, "W1A 1AB", "W1A"),
                750000,
                new DateOnly(2023, 03, 05)),
            new PropertySaleInfo(
                new Address("10 Downing Street Ext", null, "SW1A 2AA", "SW1A"),
                1200000,
                new DateOnly(2023, 07, 01)),
        };
    }

    [Fact]
    public void FilterByPostcodeArea_WithValidArea_ReturnsMatchingRecords()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.FilterByPostcodeArea("SW1A").ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(x => x.Address.PostcodeArea.Should().Be("SW1A"));
    }

    [Fact]
    public void FilterByPostcodeArea_WithNonMatchingArea_ReturnsEmpty()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.FilterByPostcodeArea("M1").ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterByPostcodeArea_WithCaseInsensitiveInput_ReturnsMatches()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.FilterByPostcodeArea("sw1a").ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void FilterByDateRange_WithValidRange_ReturnsRecordsInRange()
    {
        // Arrange
        var data = CreateTestData();
        var startDate = new DateOnly(2023, 04, 01);
        var endDate = new DateOnly(2023, 06, 30);

        // Act
        var result = data.FilterByDateRange(startDate, endDate).ToList();

        // Assert
        result.Should().HaveCount(3); // June 15, May 20, April 10
        foreach (var item in result)
        {
            item.TransactionDate.Should().HaveValue();
            item.TransactionDate.Value.CompareTo(startDate).Should().BeGreaterThanOrEqualTo(0);
            item.TransactionDate.Value.CompareTo(endDate).Should().BeLessThanOrEqualTo(0);
        }
    }

    [Fact]
    public void FilterByDateRange_WithStartDateOnly_ReturnsRecordsAfterStart()
    {
        // Arrange
        var data = CreateTestData();
        var startDate = new DateOnly(2023, 06, 01);

        // Act
        var result = data.FilterByDateRange(startDate, null).ToList();

        // Assert
        result.Should().HaveCount(2); // June 15, July 1
    }

    [Fact]
    public void FilterByDateRange_WithEndDateOnly_ReturnsRecordsBeforeEnd()
    {
        // Arrange
        var data = CreateTestData();
        var endDate = new DateOnly(2023, 05, 31);

        // Act
        var result = data.FilterByDateRange(null, endDate).ToList();

        // Assert
        result.Should().HaveCount(3); // May 20, April 10, March 5
    }

    [Fact]
    public void FilterByPriceRange_WithValidRange_ReturnsMatchingRecords()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.FilterByPriceRange(500000, 1000000).ToList();

        // Assert
        result.Should().HaveCount(2); // 850000, 750000
    }

    [Fact]
    public void FilterByPriceRange_WithMinPriceOnly_ReturnsExpensiveProperties()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.FilterByPriceRange(1000000, null).ToList();

        // Assert
        result.Should().HaveCount(2); // 1500000, 1200000
    }

    [Fact]
    public void FilterByPriceRange_WithMaxPriceOnly_ReturnsAffordableProperties()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.FilterByPriceRange(null, 500000).ToList();

        // Assert
        result.Should().HaveCount(1); // 450000
    }

    [Fact]
    public void WhereValidPrice_ReturnsOnlyValidPrices()
    {
        // Arrange
        var data = new List<PropertySaleInfo>
        {
            new PropertySaleInfo(new Address(), 100000, new DateOnly(2023, 01, 01)),
            new PropertySaleInfo(new Address(), null, new DateOnly(2023, 01, 01)),
            new PropertySaleInfo(new Address(), 0, new DateOnly(2023, 01, 01)),
            new PropertySaleInfo(new Address(), 200000, new DateOnly(2023, 01, 01)),
        };

        // Act
        var result = data.WhereValidPrice().ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(x => x.IsPriceValid.Should().BeTrue());
    }

    [Fact]
    public void WhereValidDate_ReturnsOnlyValidDates()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var data = new List<PropertySaleInfo>
        {
            new PropertySaleInfo(new Address(), 100000, pastDate),
            new PropertySaleInfo(new Address(), 100000, null),
            new PropertySaleInfo(new Address(), 100000, futureDate),
            new PropertySaleInfo(new Address(), 100000, pastDate),
        };

        // Act
        var result = data.WhereValidDate().ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(x => x.IsDateValid.Should().BeTrue());
    }

    [Fact]
    public void WhereValidPostcode_ReturnsOnlyWithValidPostcodeArea()
    {
        // Arrange
        var data = new List<PropertySaleInfo>
        {
            new PropertySaleInfo(new Address("Addr1", null, "SW1A 1AA", "SW1A"), 100000, new DateOnly(2023, 01, 01)),
            new PropertySaleInfo(new Address("Addr2", null, null, null), 100000, new DateOnly(2023, 01, 01)),
            new PropertySaleInfo(new Address("Addr3", null, "B33 8TH", "B33"), 100000, new DateOnly(2023, 01, 01)),
        };

        // Act
        var result = data.WhereValidPostcode().ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(x => x.HasValidPostcode.Should().BeTrue());
    }

    [Fact]
    public void GroupByPostcodeArea_ReturnsGroupedByArea()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var groups = data.GroupByPostcodeArea().ToList();

        // Assert
        groups.Should().HaveCount(4); // SW1A, SE1, WC2E, W1A
        groups.Should().Contain(g => g.Key == "SW1A" && g.Count() == 2);
        groups.Should().Contain(g => g.Key == "SE1" && g.Count() == 1);
    }

    [Fact]
    public void OrderByDateNewestFirst_ReturnsNewestFirst()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.OrderByDateNewestFirst().ToList();

        // Assert
        result[0].TransactionDate.Should().Be(new DateOnly(2023, 07, 01));
        result[1].TransactionDate.Should().Be(new DateOnly(2023, 06, 15));
        result[4].TransactionDate.Should().Be(new DateOnly(2023, 03, 05));
    }

    [Fact]
    public void OrderByDateOldestFirst_ReturnsOldestFirst()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.OrderByDateOldestFirst().ToList();

        // Assert
        result[0].TransactionDate.Should().Be(new DateOnly(2023, 03, 05));
        result[1].TransactionDate.Should().Be(new DateOnly(2023, 04, 10));
        result[4].TransactionDate.Should().Be(new DateOnly(2023, 07, 01));
    }

    [Fact]
    public void OrderByPriceDescending_ReturnsMostExpensiveFirst()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.OrderByPriceDescending().ToList();

        // Assert
        result[0].Price.Should().Be(1500000);
        result[1].Price.Should().Be(1200000);
        result[4].Price.Should().Be(450000);
    }

    [Fact]
    public void OrderByPriceAscending_ReturnsLeastExpensiveFirst()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.OrderByPriceAscending().ToList();

        // Assert
        result[0].Price.Should().Be(450000);
        result[1].Price.Should().Be(750000);
        result[4].Price.Should().Be(1500000);
    }

    [Fact]
    public void GetStatistics_CalculatesCorrectStats()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var stats = data.GetStatistics();

        // Assert
        stats.Count.Should().Be(5);
        stats.MinPrice.Should().Be(450000);
        stats.MaxPrice.Should().Be(1500000);
        stats.AvgPrice.Should().BeApproximately(950000, 1);
    }

    [Fact]
    public void GetStatistics_WithNoValidPrices_ReturnsNullStats()
    {
        // Arrange
        var data = new List<PropertySaleInfo>
        {
            new PropertySaleInfo(new Address(), null, new DateOnly(2023, 01, 01)),
            new PropertySaleInfo(new Address(), 0, new DateOnly(2023, 01, 01)),
            new PropertySaleInfo(new Address(), -1000, new DateOnly(2023, 01, 01)),
        };

        // Act
        var stats = data.GetStatistics();

        // Assert
        stats.Count.Should().Be(3);
        stats.AvgPrice.Should().BeNull();
        stats.MinPrice.Should().BeNull();
        stats.MaxPrice.Should().BeNull();
    }
}
