using System;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.VisualBasic;
using TotalCommander.Interface.Abstraction.FileSystem.Native.Converter;
using TotalCommander.Interface.Abstraction.FileSystem.Native.Models;
using Xunit;
using Directory = TotalCommander.Interface.Abstraction.FileSystem.Models.Directory;
using File = TotalCommander.Interface.Abstraction.FileSystem.Models.File;

namespace TotalCommander.Interface.Abstraction.Tests.FileSystem.Models;

public class ModelsTests
{
    [Fact]
    public void DirectoryAsNative_ShouldWork()
    {
        // Arrange
        const string name = "name";

        var directory = new Directory(name);

        var allocHGlobal = Marshal.AllocHGlobal(Marshal.SizeOf<AnsiTcFindData>());

        // Act
        directory.AsNative().CopyTo(allocHGlobal, isUnicode: false);

        // Assert
        var ansiTcFindData = Marshal.PtrToStructure<AnsiTcFindData>(allocHGlobal);

        var defaultTime = ((DateTime?)null).ToFileTime();

        var expected = new AnsiTcFindData
        {
            CreationTime = defaultTime,
            FileAttributes = (int)FileAttributes.Directory,
            LastAccessTime = defaultTime,
            LastWriteTime = defaultTime,
            FileName = name,
            AlternateFileName = string.Empty
        };

        ansiTcFindData.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void FileAsNative_ShouldWork()
    {
        // Arrange
        const string name = "name";
        const ulong size = 10;

        var file = new File(name, size);

        var allocHGlobal = Marshal.AllocHGlobal(Marshal.SizeOf<AnsiTcFindData>());

        // Act
        file.AsNative().CopyTo(allocHGlobal, isUnicode: false);

        // Assert
        var ansiTcFindData = Marshal.PtrToStructure<AnsiTcFindData>(allocHGlobal);

        var defaultTime = ((DateTime?)null).ToFileTime();

        var expected = new AnsiTcFindData
        {
            CreationTime = defaultTime,
            FileAttributes = (int)FileAttributes.Normal,
            LastAccessTime = defaultTime,
            LastWriteTime = defaultTime,
            FileName = name,
            AlternateFileName = string.Empty,
            FileSizeHigh = NumberConverter.GetHigh(size),
            FileSizeLow = NumberConverter.GetLow(size)
        };

        ansiTcFindData.Should().BeEquivalentTo(expected);
    }
}
