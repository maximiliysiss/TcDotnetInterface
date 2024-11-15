using TotalCommander.Interface.Aot.Context.Models;

namespace TotalCommander.Interface.Aot.Context.Plugins;

internal abstract record Plugin(string Name, Method[] Methods, Extension[] Extensions);
