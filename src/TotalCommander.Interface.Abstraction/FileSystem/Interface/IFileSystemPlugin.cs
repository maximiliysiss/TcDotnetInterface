using System.Collections.Generic;
using TotalCommander.Interface.Abstraction.FileSystem.Models;

namespace TotalCommander.Interface.Abstraction.FileSystem.Interface;

public interface IFileSystemPlugin
{
    IEnumerable<Entry> EnumerateEntries(string path);
}
