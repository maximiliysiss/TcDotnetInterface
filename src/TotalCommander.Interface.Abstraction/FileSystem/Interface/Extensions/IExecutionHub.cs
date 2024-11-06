using TotalCommander.Interface.Abstraction.FileSystem.Interface.Extensions.Models;

namespace TotalCommander.Interface.Abstraction.FileSystem.Interface.Extensions;

public interface IExecutionHub
{
    ExecuteResult Execute(string path, string command);
}
