using FluentAssertions;
using TotalCommander.Interface.Abstraction.FileSystem.Native.Converter;
using Xunit;

namespace TotalCommander.Interface.Abstraction.Tests.FileSystem.Converters;

public class NumberConverterTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0xffffffff, 0)]
    [InlineData(0xfffffffff, 15)]
    public void LongGetHigh_ShouldWorkCorrect(long value, int expected)
    {
        // Arrange

        // Act
        var high = NumberConverter.GetHigh(value);

        // Assert
        high.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0xffffffff, 0)]
    [InlineData(0xfffffffff, 15)]
    public void ULongGetHigh_ShouldWorkCorrect(ulong value, uint expected)
    {
        // Arrange

        // Act
        var high = NumberConverter.GetHigh(value);

        // Assert
        high.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(0xffffffff, -1)]
    [InlineData(0xfffffffff, -1)]
    public void LongGetLow_ShouldWorkCorrect(long value, int expected)
    {
        // Arrange

        // Act
        var high = NumberConverter.GetLow(value);

        // Assert
        high.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(0xffffffff, 0xffffffff)]
    [InlineData(0xfffffffff, 0xffffffff)]
    public void ULongGetLow_ShouldWorkCorrect(ulong value, uint expected)
    {
        // Arrange

        // Act
        var high = NumberConverter.GetLow(value);

        // Assert
        high.Should().Be(expected);
    }
}
