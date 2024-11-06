using System.IO;

namespace TotalCommander.Interface.Abstraction.FileSystem.Models;

public sealed class Directory(string name) : Entry
{
    public override Native.Models.Entry AsNative() => new(name, FileAttributes.Directory);
}
