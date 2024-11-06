using System;
using System.Runtime.InteropServices.ComTypes;
using FluentAssertions;
using TotalCommander.Interface.Abstraction.FileSystem.Native.Converter;
using Xunit;

namespace TotalCommander.Interface.Abstraction.Tests.FileSystem.Converters;

public class FileTimeConverterTests
{
    [Fact]
    public void ToFileTime_ShouldConvert_WhenIsNull()
    {
        // Arrange
        DateTime? dateTime = null;

        // Act
        var fileTime = dateTime.ToFileTime();

        // Assert
        const long numberBase = long.MaxValue << 1;

        var expected = new FILETIME
        {
            dwHighDateTime = NumberConverter.GetHigh(numberBase),
            dwLowDateTime = NumberConverter.GetLow(numberBase)
        };

        fileTime.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToFileTime_ShouldConvert_WhenIsNotNull()
    {
        // Arrange
        DateTime? dateTime = DateTime.UtcNow;

        // Act
        var fileTime = dateTime.ToFileTime();

        // Assert
        var numberBase = dateTime.Value.ToFileTime();

        var expected = new FILETIME
        {
            dwHighDateTime = NumberConverter.GetHigh(numberBase),
            dwLowDateTime = NumberConverter.GetLow(numberBase)
        };

        fileTime.Should().BeEquivalentTo(expected);
    }
}
