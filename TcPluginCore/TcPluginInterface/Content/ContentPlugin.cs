using System;
using System.Collections.Specialized;
using System.Globalization;

namespace TcPluginInterface.Content;

public class ContentPlugin : TcPlugin, IContentPlugin
{
    #region Constructors

    public ContentPlugin(StringDictionary pluginSettings) : base(pluginSettings)
    {
    }

    #endregion Constructors

    #region Callback Procedures

    protected int ProgressProc(int nextBlockData) => OnTcPluginEvent(new ContentProgressEventArgs(nextBlockData));

    #endregion Callback Procedures

    #region Properties

    public virtual string DetectString { get; set; }

    public override string TraceTitle =>
        Convert.ToBoolean(Settings["useTitleForTrace"]) || PluginNumber == 0 ? Title : PluginNumber.ToString(CultureInfo.InvariantCulture);

    #endregion Properties

    #region IContentPlugin Members

    #region Mandatory Methods

    public virtual ContentFieldType GetSupportedField(int fieldIndex, out string fieldName, out string units, int maxLen) =>
        throw new MethodNotSupportedException("GetSupportedField", true);

    public virtual GetValueResult GetValue(
        string fileName,
        int fieldIndex,
        int unitIndex,
        int maxLen,
        GetValueFlags flags,
        out string fieldValue,
        out ContentFieldType fieldType) =>
        throw new MethodNotSupportedException("GetValue", true);

    #endregion Mandatory Methods

    #region Optional Methods

    // methods used in both WDX and WFX plugins
    public virtual void StopGetValue(string fileName)
    {
    }

    public virtual DefaultSortOrder GetDefaultSortOrder(int fieldIndex) => DefaultSortOrder.Asc;

    public virtual void PluginUnloading()
    {
    }

    public virtual SupportedFieldOptions GetSupportedFieldFlags(int fieldIndex) => SupportedFieldOptions.None;

    public virtual SetValueResult SetValue(
        string fileName,
        int fieldIndex,
        int unitIndex,
        ContentFieldType fieldType,
        string fieldValue,
        SetValueFlags flags) =>
        SetValueResult.NoSuchField;

    // method used in WFX plugins only
    public virtual bool GetDefaultView(
        out string viewContents,
        out string viewHeaders,
        out string viewWidths,
        out string viewOptions,
        int maxLen)
    {
        viewContents = null;
        viewHeaders = null;
        viewWidths = null;
        viewOptions = null;
        return false;
    }

    // methods used in WDX plugins only
    public virtual SetValueResult EditValue(
        TcWindow parentWin,
        int fieldIndex,
        int unitIndex,
        ContentFieldType fieldType,
        ref string fieldValue,
        int maxLen,
        EditValueFlags flags,
        string langIdentifier) =>
        SetValueResult.NoSuchField;

    public virtual void SendStateInformation(StateChangeInfo state, string path)
    {
    }

    public virtual ContentCompareResult CompareFiles(
        int compareIndex,
        string fileName1,
        string fileName2,
        ContentFileDetails contentFileDetails,
        out int iconResourceId)
    {
        iconResourceId = 0;
        return ContentCompareResult.CannotCompare;
    }

    #endregion Optional Methods

    #endregion IContentPlugin Members
}
