namespace TcPluginInterface.Packer;

public class PackerPassword : PluginPassword
{
    public PackerPassword(TcPlugin plugin, int cryptoNumber, int flags) : base(plugin, cryptoNumber, flags)
    {
    }

    protected override CryptResult GetCryptResult(int tcCryptResult) =>
        tcCryptResult switch
        {
            (int)PackerResult.Ok => CryptResult.Ok,
            (int)PackerResult.ErrorCreate => CryptResult.Failed,
            (int)PackerResult.NoFiles => CryptResult.NoMasterPassword,
            (int)PackerResult.ErrorRead => CryptResult.PasswordNotFound,
            (int)PackerResult.ErrorWrite => CryptResult.WriteError,
            _ => CryptResult.PasswordNotFound
        };
}
