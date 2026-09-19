namespace UrlShortener.Tests.Domain;

using FluentAssertions;
using UrlShortener.Domain.Common;
using Xunit;

public class Base62ConverterTests
{
    [Theory]
    [InlineData(1, "1")]
    [InlineData(61, "Z")]
    [InlineData(62, "10")]
    [InlineData(10000000, "FXsk")]
    [InlineData(long.MaxValue, "aZl8N0y58M7")]
    public void Encode_ValidNumbers_ShouldReturnExpectedBase62String(long input, string expected)
    {
        // Act
        string result = Base62Converter.Encode(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999999)]
    public void Encode_ZeroOrNegativeNumbers_ShouldThrowArgumentOutOfRangeException(long invalidInput)
    {
        // Act
        var act = () => Base62Converter.Encode(invalidInput);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(62)]
    [InlineData(10000000)]
    [InlineData(4839201928301L)]
    [InlineData(long.MaxValue)]
    public void RoundTrip_EncodeThenDecode_ShouldReturnOriginalValue(long original)
    {
        // Act
        string encoded = Base62Converter.Encode(original);
        long decoded = Base62Converter.Decode(encoded);

        // Assert
        decoded.Should().Be(original);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Decode_EmptyOrWhitespace_ShouldThrowArgumentException(string emptyCode)
    {
        // Act
        var act = () => Base62Converter.Decode(emptyCode);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decode_InvalidCharacters_ShouldThrowFormatException()
    {
        // Act
        var act = () => Base62Converter.Decode("abc#123");

        // Assert
        act.Should().Throw<FormatException>();
    }
}