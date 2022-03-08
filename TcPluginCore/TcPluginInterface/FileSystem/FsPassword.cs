using System;

namespace TcPluginInterface.FileSystem;

[Serializable]
public class FsPassword : PluginPassword
{
    public FsPassword(TcPlugin plugin, int cryptoNumber, int flags) : base(plugin, cryptoNumber, flags)
    {
    }

    protected override CryptResult GetCryptResult(int tcCryptResult) =>
        tcCryptResult switch
        {
            (int)FileSystemExitCode.Ok => CryptResult.Ok,
            (int)FileSystemExitCode.NotSupported => CryptResult.Failed,
            (int)FileSystemExitCode.FileNotFound => CryptResult.NoMasterPassword,
            (int)FileSystemExitCode.ReadError => CryptResult.PasswordNotFound,
            (int)FileSystemExitCode.WriteError => CryptResult.WriteError,
            _ => CryptResult.PasswordNotFound
        };
}
