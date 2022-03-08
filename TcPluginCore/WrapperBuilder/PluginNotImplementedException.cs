using System;
using System.Runtime.Serialization;

namespace WrapperBuilder;

[Serializable]
public class PluginNotImplementedException : Exception
{
    public PluginNotImplementedException()
    {
    }

    public PluginNotImplementedException(string message) : base(message)
    {
    }

    public PluginNotImplementedException(string message, Exception inner) : base(message, inner)
    {
    }

    protected PluginNotImplementedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
