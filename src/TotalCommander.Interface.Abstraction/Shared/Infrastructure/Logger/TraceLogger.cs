using System.Diagnostics;

namespace TotalCommander.Interface.Abstraction.Shared.Infrastructure.Logger;

internal sealed class TraceLogger(string path) : ILogger
{
    public void Log(string message) => Trace.WriteLine(message, path);
}
