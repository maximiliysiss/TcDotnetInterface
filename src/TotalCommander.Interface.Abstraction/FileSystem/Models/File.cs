using System.IO;

namespace TotalCommander.Interface.Abstraction.FileSystem.Models;

public sealed class File(string name, ulong size) : Entry
{
    public override Native.Models.Entry AsNative() => new(name, FileAttributes.Normal, size);
}
