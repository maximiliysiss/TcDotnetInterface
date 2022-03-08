using System.Collections.Specialized;

namespace TcPluginInterface.QuickSearch;

public class QuickSearchPlugin : TcPlugin, IQuickSearchPlugin
{
    #region Constructors

    public QuickSearchPlugin(StringDictionary pluginSettings) : base(pluginSettings)
    {
    }

    #endregion Constructors

    #region IQSPlugin Members

    public virtual bool MatchFile(string filter, string fileName) => false;

    public virtual MatchOptions MatchGetSetOptions(ExactNameMatch status) => MatchOptions.None;

    #endregion IQSPlugin Members
}
