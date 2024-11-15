using Microsoft.CodeAnalysis;
using TotalCommander.Interface.Aot.Context.Plugins;

namespace TotalCommander.Interface.Aot.Receivers.Models;

internal sealed record PluginReceiverContext(INamedTypeSymbol Symbol, Location Location, Plugin[] Plugins);
