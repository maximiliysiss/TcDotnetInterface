using System;

namespace TcPluginInterface;

// Class, used to store passwords in the TC secure password store, 
// retrieve them back, or copy them to a new store.
// It's a parent class for FsPassword and PackerPassword clases.
[Serializable]
public class PluginPassword
{
    private readonly int _cryptoNumber;
    private readonly CryptFlags _flags;
    private TcPlugin _plugin;

    public PluginPassword(TcPlugin plugin, int cryptoNumber, int flags)
    {
        _plugin = plugin;
        _cryptoNumber = cryptoNumber;
        _flags = (CryptFlags)flags;
    }

    public bool TcMasterPasswordDefined => (_flags & CryptFlags.MasterPassSet) == CryptFlags.MasterPassSet;

    // Convert result returned by TC to CryptResult. Must be overidden in derived classes.
    protected virtual CryptResult GetCryptResult(int tcCryptResult) => CryptResult.PasswordNotFound;

    private CryptResult Crypt(CryptMode mode, string storeName, ref string password)
    {
        var e =
            new CryptEventArgs(_plugin.PluginNumber, _cryptoNumber, (int)mode, storeName, password);
        var result = GetCryptResult(_plugin.OnTcPluginEvent(e));
        if (result == CryptResult.Ok)
        {
            password = e.Password;
        }

        return result;
    }

    #region Public Methods

    // Save password to password store.
    public CryptResult Save(string store, string password) => Crypt(CryptMode.SavePassword, store, ref password);

    // Load password from password store.
    public CryptResult Load(string store, ref string password)
    {
        password = string.Empty;
        return Crypt(CryptMode.LoadPassword, store, ref password);
    }

    // Load password from password store only if master password has already been entered.
    public CryptResult LoadNoUi(string store, ref string password)
    {
        password = string.Empty;
        return Crypt(CryptMode.LoadPasswordNoUi, store, ref password);
    }

    // Copy password to new store.
    public CryptResult Copy(string sourceStore, string targetStore) => Crypt(CryptMode.CopyPassword, sourceStore, ref targetStore);

    // Copy password to new store and delete the source password.
    public CryptResult Move(string sourceStore, string targetStore) => Crypt(CryptMode.MovePassword, sourceStore, ref targetStore);

    // Delete the password of the given store.
    public CryptResult Delete(string store)
    {
        var password = string.Empty;
        return Crypt(CryptMode.DeletePassword, store, ref password);
    }

    public int GetCryptoNumber() => _cryptoNumber;

    public int GetFlags() => (int)_flags;

    #endregion Public Methods

    #region Private Enumerations

    [Flags]
    private enum CryptFlags
    {
        None = 0,
        MasterPassSet = 1 // The user already has a master password defined.
    }

    private enum CryptMode
    {
        SavePassword = 1,
        LoadPassword,
        LoadPasswordNoUi,
        CopyPassword,
        MovePassword,
        DeletePassword
    }

    #endregion Private Enumerations
}
