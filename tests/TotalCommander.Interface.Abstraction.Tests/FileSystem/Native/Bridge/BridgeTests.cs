using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Moq;
using TotalCommander.Interface.Abstraction.FileSystem.Interface;
using TotalCommander.Interface.Abstraction.FileSystem.Interface.Extensions;
using TotalCommander.Interface.Abstraction.FileSystem.Interface.Extensions.Models;
using TotalCommander.Interface.Abstraction.FileSystem.Models;
using TotalCommander.Interface.Abstraction.FileSystem.Native.Models;
using Xunit;
using Entry = TotalCommander.Interface.Abstraction.FileSystem.Models.Entry;

namespace TotalCommander.Interface.Abstraction.Tests.FileSystem.Native.Bridge;

public class BridgeTests
{
    [Fact]
    public void FindFirst_ShouldReturnValidValue_WhenEmpty()
    {
        // Arrange
        const string path = "some-path";

        var plugin = new Mock<IFileSystemPlugin>(MockBehavior.Strict);
        plugin
            .Setup(c => c.EnumerateEntries(path))
            .Returns([]);

        var bridge = Create(plugin);

        // Act
        var outputPtr = bridge.FindFirst(path: path, findFile: IntPtr.Zero, isUnicode: true);

        // Assert
        outputPtr.Should().Be(-1);
    }

    [Fact]
    public void FindFirst_ShouldReturnValidValue_WhenHasElements()
    {
        // Arrange
        const string path = "some-path";
        const string someDirectory = "some-directory";

        var directory = new Directory(someDirectory);
        Entry[] enumerable = [directory];

        var plugin = new Mock<IFileSystemPlugin>(MockBehavior.Strict);
        plugin
            .Setup(c => c.EnumerateEntries(path))
            .Returns(enumerable);

        var bridge = Create(plugin);

        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<UnicodeTcFindData>());

        // Act
        var outputPtr = bridge.FindFirst(path: path, findFile: ptr, isUnicode: true);

        // Assert
        outputPtr.Should().NotBe(-1);

        var data = Marshal.PtrToStructure<UnicodeTcFindData>(ptr);

        data.FileName.Should().Be(someDirectory);
    }

    [Fact]
    public void Find_ShouldReturnValidValue_WhenHasElements()
    {
        // Arrange
        const string path = "some-path";
        const string directoryName = "some-directory";
        const string fileName = "file-name";

        var directory = new Directory(directoryName);
        var file = new File(fileName, 10);

        Entry[] enumerable = [directory, file];

        var plugin = new Mock<IFileSystemPlugin>(MockBehavior.Strict);
        plugin
            .Setup(c => c.EnumerateEntries(path))
            .Returns(enumerable);

        var bridge = Create(plugin);

        var firstFind = Marshal.AllocHGlobal(Marshal.SizeOf<UnicodeTcFindData>());
        var secondFind = Marshal.AllocHGlobal(Marshal.SizeOf<UnicodeTcFindData>());

        // Act
        var outputPtr = bridge.FindFirst(path: path, findFile: firstFind, isUnicode: true);
        bridge.FindNext(outputPtr, secondFind, true);
        var isFoundNext = bridge.FindNext(outputPtr, IntPtr.Zero, true);

        // Assert
        isFoundNext.Should().BeFalse();

        var firstData = Marshal.PtrToStructure<UnicodeTcFindData>(firstFind);
        var secondData = Marshal.PtrToStructure<UnicodeTcFindData>(secondFind);

        firstData.FileName.Should().Be(directoryName);
        secondData.FileName.Should().Be(fileName);
    }

    [Fact]
    public void DeleteFile_ShouldCallDeleting()
    {
        // Arrange
        const string path = "some-path";

        var plugin = new Mock<IFileSystemPlugin>(MockBehavior.Strict);
        plugin
            .As<IFileHub>()
            .Setup(c => c.Delete(path));

        var bridge = Create(plugin);

        // Act
        var outputPtr = bridge.DeleteFile(path);

        // Assert
        outputPtr.Should().BeTrue();

        plugin.VerifyAll();
    }

    [Fact]
    public void RemoveDirectory_ShouldCallDeleting()
    {
        // Arrange
        const string path = "some-path";

        var plugin = new Mock<IFileSystemPlugin>(MockBehavior.Strict);
        plugin
            .As<IDirectoryHub>()
            .Setup(c => c.Delete(path));

        var bridge = Create(plugin);

        // Act
        var outputPtr = bridge.RemoveDirectory(path);

        // Assert
        outputPtr.Should().BeTrue();

        plugin.VerifyAll();
    }

    [Fact]
    public void CreateDirectory_ShouldCallDeleting()
    {
        // Arrange
        const string path = "some-path";

        var plugin = new Mock<IFileSystemPlugin>(MockBehavior.Strict);
        plugin
            .As<IDirectoryHub>()
            .Setup(c => c.Create(path));

        var bridge = Create(plugin);

        // Act
        var outputPtr = bridge.CreateDirectory(path);

        // Assert
        outputPtr.Should().BeTrue();

        plugin.VerifyAll();
    }

    [Fact]
    public void Execute_ShouldCallExecute_ByOpen()
    {
        // Arrange
        const string path = "some-path";

        var plugin = new Mock<IFileSystemPlugin>(MockBehavior.Strict);
        plugin
            .As<IFileHub>()
            .Setup(c => c.Open(path));

        var bridge = Create(plugin);

        // Act
        var outputPtr = bridge.Execute(path, "open");

        // Assert
        outputPtr.Should().Be(0);

        plugin.VerifyAll();
    }

    [Fact]
    public void Execute_ShouldCallExecute_ByExecute()
    {
        // Arrange
        const string path = "some-path";
        const string command = "touch some.file";

        var plugin = new Mock<IFileSystemPlugin>(MockBehavior.Strict);
        plugin
            .As<IExecutionHub>()
            .Setup(c => c.Execute(path, command))
            .Returns(ExecuteResult.Success);

        var bridge = Create(plugin);

        // Act
        var outputPtr = bridge.Execute(path, $"quote {command}");

        // Assert
        outputPtr.Should().Be(0);

        plugin.VerifyAll();
    }

    private static Abstraction.FileSystem.Native.Bridge.Bridge Create(Mock<IFileSystemPlugin> plugin) => new(plugin.Object);
}
