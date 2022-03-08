using System;

namespace TcPluginInterface.Lister;

public interface IListerHandlerBuilder
{
    ListerPlugin Plugin { get; set; }
    IntPtr GetHandle(object listerControl, IntPtr parentHandle);
}
