using System.Collections.Generic;
using TotalCommander.Interface.Abstraction.FileSystem.Interface;
using TotalCommander.Interface.Abstraction.FileSystem.Interface.Extensions;
using TotalCommander.Interface.Abstraction.FileSystem.Models;

namespace TotalCommander.Interface.Aot.Tests.Shared;

public sealed class FileSystemPlugin : IFileSystemPlugin, IFileHub
{
    public IEnumerable<Entry> EnumerateEntries(string path) => throw new System.NotImplementedException();

    public void Create(string path) => throw new System.NotImplementedException();

    public void Delete(string path) => throw new System.NotImplementedException();

    public void Open(string path) => throw new System.NotImplementedException();
}
