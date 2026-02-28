using FluentAssertions;
using PropertyPrices.Core.Models;
using PropertyPrices.Core.Transformations;
using Xunit;

namespace PropertyPrices.Tests.Transformations;

public class PropertySaleTransformerTests
{
    [Fact]
    public void Transform_WithValidPropertySaleRecord_CreatesPropertySaleInfo()
    {
        // Arrange
        var record = new PropertySaleRecord
        {
            Address = "10 Downing Street",
            Postcode = "SW1A 1AA",
            Price = 1500000,
            TransactionDate = new DateOnly(2023, 06, 15),
            RetrievedAt = DateTime.UtcNow
        };

        // Act
        var result = PropertySaleTransformer.Transform(record);

        // Assert
        result.Should().NotBeNull();
        result.Address.StreetName.Should().Be("10 Downing Street");
        result.Address.Postcode.Should().Be("SW1A 1AA");
        result.Address.PostcodeArea.Should().Be("SW1A");
        result.Price.Should().Be(1500000);
        result.TransactionDate.Should().Be(new DateOnly(2023, 06, 15));
        result.IsPriceValid.Should().BeTrue();
        result.IsDateValid.Should().BeTrue();
        result.HasValidPostcode.Should().BeTrue();
    }

    [Fact]
    public void Transform_WithNullRecord_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => PropertySaleTransformer.Transform(null!));
        ex.ParamName.Should().Be("record");
    }

    [Fact]
    public void Transform_WithNullPrice_CreatesPropertySaleInfoWithNullPrice()
    {
        // Arrange
        var record = new PropertySaleRecord
        {
            Address = "10 Downing Street",
            Postcode = "SW1A 1AA",
            Price = null,
            TransactionDate = new DateOnly(2023, 06, 15)
        };

        // Act
        var result = PropertySaleTransformer.Transform(record);

        // Assert
        result.Price.Should().BeNull();
        result.IsPriceValid.Should().BeFalse();
    }

    [Fact]
    public void Transform_WithZeroPrice_CreatesPropertySaleInfoWithNullPrice()
    {
        // Arrange
        var record = new PropertySaleRecord
        {
            Address = "Test Street",
            Postcode = "SW1A 1AA",
            Price = 0,
            TransactionDate = new DateOnly(2023, 06, 15)
        };

        // Act
        var result = PropertySaleTransformer.Transform(record);

        // Assert
        result.Price.Should().BeNull();
        result.IsPriceValid.Should().BeFalse();
    }

    [Fact]
    public void Transform_WithNegativePrice_CreatesPropertySaleInfoWithNullPrice()
    {
        // Arrange
        var record = new PropertySaleRecord
        {
            Address = "Test Street",
            Postcode = "SW1A 1AA",
            Price = -1000,
            TransactionDate = new DateOnly(2023, 06, 15)
        };

        // Act
        var result = PropertySaleTransformer.Transform(record);

        // Assert
        result.Price.Should().BeNull();
        result.IsPriceValid.Should().BeFalse();
    }

    [Fact]
    public void Transform_WithFutureDate_CreatesPropertySaleInfoWithNullDate()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var record = new PropertySaleRecord
        {
            Address = "Test Street",
            Postcode = "SW1A 1AA",
            Price = 500000,
            TransactionDate = futureDate
        };

        // Act
        var result = PropertySaleTransformer.Transform(record);

        // Assert
        result.TransactionDate.Should().BeNull();
        result.IsDateValid.Should().BeFalse();
    }

    [Fact]
    public void ParsePostcodeArea_WithValidPostcode_ExtractsAreaCorrectly()
    {
        // Test cases: (input, expected_area)
        var testCases = new[]
        {
            ("SW1A 1AA", "SW1A"),
            ("sw1a 1aa", "SW1A"),
            ("B33 8TH", "B33"),
            ("CR2 6XH", "CR2"),
            ("EC1A 1BB", "EC1A"),
            ("W1A 0AX", "W1A"),
            ("M1 1AE", "M1"),
        };

        foreach (var (input, expected) in testCases)
        {
            // Act
            var result = PropertySaleTransformer.ParsePostcodeArea(input);

            // Assert
            result.Should().Be(expected, $"for input '{input}'");
        }
    }

    [Fact]
    public void ParsePostcodeArea_WithInvalidPostcode_ReturnsNull()
    {
        // Test cases: invalid postcodes
        var testCases = new[] { "", "   ", null, "INVALID", "123456" };

        foreach (var input in testCases)
        {
            // Act
            var result = PropertySaleTransformer.ParsePostcodeArea(input);

            // Assert
            result.Should().BeNull($"for input '{input}'");
        }
    }

    [Fact]
    public void ValidatePrice_WithPositivePrice_ReturnsPrice()
    {
        // Act
        var result = PropertySaleTransformer.ValidatePrice(250000);

        // Assert
        result.Should().Be(250000);
    }

    [Fact]
    public void ValidatePrice_WithNullPrice_ReturnsNull()
    {
        // Act
        var result = PropertySaleTransformer.ValidatePrice(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidatePrice_WithZeroOrNegativePrice_ReturnsNull()
    {
        // Act
        var result0 = PropertySaleTransformer.ValidatePrice(0);
        var resultNegative = PropertySaleTransformer.ValidatePrice(-1000);

        // Assert
        result0.Should().BeNull();
        resultNegative.Should().BeNull();
    }

    [Fact]
    public void ValidateTransactionDate_WithPastDate_ReturnsDate()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

        // Act
        var result = PropertySaleTransformer.ValidateTransactionDate(pastDate);

        // Assert
        result.Should().Be(pastDate);
    }

    [Fact]
    public void ValidateTransactionDate_WithTodayDate_ReturnsDate()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = PropertySaleTransformer.ValidateTransactionDate(today);

        // Assert
        result.Should().Be(today);
    }

    [Fact]
    public void ValidateTransactionDate_WithFutureDate_ReturnsNull()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Act
        var result = PropertySaleTransformer.ValidateTransactionDate(futureDate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateTransactionDate_WithNullDate_ReturnsNull()
    {
        // Act
        var result = PropertySaleTransformer.ValidateTransactionDate(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void NormalizePostcode_WithValidPostcode_ReturnsNormalizedPostcode()
    {
        // Act
        var result = PropertySaleTransformer.NormalizePostcode("sw1a 1aa");

        // Assert
        result.Should().Be("SW1A 1AA");
    }

    [Fact]
    public void NormalizePostcode_WithWhitespace_TrimsAndNormalizes()
    {
        // Act
        var result = PropertySaleTransformer.NormalizePostcode("  sw1a 1aa  ");

        // Assert
        result.Should().Be("SW1A 1AA");
    }

    [Fact]
    public void NormalizePostcode_WithInvalidPostcode_ReturnsNull()
    {
        // Act
        var result = PropertySaleTransformer.NormalizePostcode("invalid");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TransformBulk_WithMultipleRecords_TransformsAllRecords()
    {
        // Arrange
        var records = new[]
        {
            new PropertySaleRecord { Address = "Address 1", Postcode = "SW1A 1AA", Price = 100000, TransactionDate = new DateOnly(2023, 01, 01) },
            new PropertySaleRecord { Address = "Address 2", Postcode = "B33 8TH", Price = 200000, TransactionDate = new DateOnly(2023, 02, 01) },
            new PropertySaleRecord { Address = "Address 3", Postcode = "EC1A 1BB", Price = 300000, TransactionDate = new DateOnly(2023, 03, 01) },
        };

        // Act
        var results = PropertySaleTransformer.TransformBulk(records);

        // Assert
        results.Should().HaveCount(3);
        results[0].Address.PostcodeArea.Should().Be("SW1A");
        results[1].Address.PostcodeArea.Should().Be("B33");
        results[2].Address.PostcodeArea.Should().Be("EC1A");
    }

    [Fact]
    public void TransformBulk_WithNullRecordInList_SkipsNullAndTransformsOthers()
    {
        // Arrange
        var records = new PropertySaleRecord?[]
        {
            new PropertySaleRecord { Address = "Address 1", Postcode = "SW1A 1AA", Price = 100000, TransactionDate = new DateOnly(2023, 01, 01) },
            null,
            new PropertySaleRecord { Address = "Address 2", Postcode = "B33 8TH", Price = 200000, TransactionDate = new DateOnly(2023, 02, 01) },
        };

        // Act
        var results = PropertySaleTransformer.TransformBulk(records.Where(x => x != null)!);

        // Assert
        results.Should().HaveCount(2);
    }
}
