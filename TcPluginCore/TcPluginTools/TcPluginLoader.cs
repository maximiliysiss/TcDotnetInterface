using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Threading;
using System.Windows.Forms;
using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.Content;
using OY.TotalCommander.TcPluginInterface.FileSystem;
using OY.TotalCommander.TcPluginInterface.Lister;
#if TRACE
using System.Globalization;
#endif
#if TRACE
using System.Text;
#endif

namespace OY.TotalCommander.TcPluginTools;

[Serializable]
public static class TcPluginLoader
{
    #region TC Plugin Event Handler

    private static void HandleTcPluginEvent(object sender, PluginEventArgs e)
    {
        // This TC Plugin event handler is called in plugin AppDomain
        var tp = sender as TcPlugin;
        if (tp == null)
        {
            throw new ArgumentException(ErrorMsg3);
        }

        // First we pass event arguments to main AppDomain  
        var callBackDataBufferName = tp.DataBufferName;

        var mainDomain = tp.MainDomain;
        mainDomain.SetData(TcCallback.PluginCallbackDataName, tp.PluginId);
        mainDomain.SetData(callBackDataBufferName, e);
        PluginEventArgs res;
        try
        {
            // ... then call event handler in main AppDomain
            mainDomain.DoCallBack(TcCallback.TcPluginCallbackHandler);
            // ... and then check result returned from TC (if exists)
            res = (PluginEventArgs)mainDomain.GetData(callBackDataBufferName);
        }
        finally
        {
            mainDomain.SetData(callBackDataBufferName, null);
        }

        if (e.GetType() != res.GetType())
        {
            throw new InvalidOperationException(ErrorMsg4);
        }

        e.Result = res.Result;

        // Following part is event specific !!!
        if (e is RequestEventArgs)
        {
            ((RequestEventArgs)e).ReturnedText = ((RequestEventArgs)res).ReturnedText;
        }
        else if (e is CryptEventArgs)
        {
            ((CryptEventArgs)e).Password = ((CryptEventArgs)res).Password;
        }
    }

    #endregion TC Plugin Event Handler

    #region Constants

    // Error Messages
    private const string ErrorMsg1 = "Could not create TC Plugin object for {0}.";
    private const string ErrorMsg2 = "Could not find TC Plugin interface.";
    private const string ErrorMsg3 = "Invalid sender for TcPlugin event.";
    private const string ErrorMsg4 = "Callback response type problem.";
    private const string ErrorMsg5 = "Could not find class {0}.";

    private const string LifetimeExpiredMsgEnd = "has been disconnected or does not exist at the server.";

    // Trace Messages
#if TRACE
    private const string TraceMsg1 = "Plugin Wrapper: {0}.";
    private const string TraceMsg2 = "Start";
    private const string TraceMsg3 = "Plugin assembly";
    private const string TraceMsg4 = "{0}. \"{1}\" [BaseDir={2}].";
    private const string TraceMsg5 = "App.Domain created";
    private const string TraceMsg6 = "\"{0}\" [Type={1}].";
    private const string TraceMsg7 = "{0} plugin loaded";
    private const string TraceMsg8 = "  Content assembly";
    private const string TraceMsg9 = "  No Content plugin loaded";
    private const string TraceMsg10 = "  Content interface implemented in FS type.";
    private const string TraceMsg11 = "[Type={0}].";
    private const string TraceMsg12 = "  Content plugin loaded";
    private const string TraceMsg13 = "  Could not load Content Plugin {0}: {1}.";
    private const string TraceMsg14 = "Plugin \"{0}\" - connection to TC restored.";
    private const string TraceMsg15 = "Plugin Lifetime Expired - {0}.{1}";
#endif

    #endregion Constants

    #region Variables

    private static List<TcPluginLoadingInfo> loadedPlugins = new();
    private static StringDictionary pluginSettings;
    private static IntPtr tcMainWindowHandle = IntPtr.Zero;
#if TRACE
    private static bool writeTrace;
#endif

    #endregion Variables

    #region Properties

    // Domain where plugin wrapper is started in.
    private static readonly AppDomain MainDomain = AppDomain.CurrentDomain;

    public static string DomainInfo
    {
        get
        {
#if TRACE
            var sb = new StringBuilder();
            var domain = AppDomain.CurrentDomain;
            if (domain.Equals(MainDomain))
            {
                sb.AppendFormat("\nApp Domain: {0}.{1}", domain.Id, domain.FriendlyName);
                sb.Append("\n  Assemblies loaded:");
                foreach (var a in domain.GetAssemblies())
                {
                    var aName = a.GetName().Name;
                    if (aName.Equals("<unknown>"))
                    {
                        sb.AppendFormat("\n\t{0}", aName);
                    }
                    else
                    {
                        if (a.GlobalAssemblyCache)
                        {
                            sb.AppendFormat("\n\t{{ GAC }} {0}", Path.GetFileName(a.Location));
                        }
                        else if (string.IsNullOrEmpty(a.Location))
                        {
                            sb.AppendFormat("\n\t[{0}]", a.FullName);
                        }
                        else
                        {
                            sb.AppendFormat("\n\t{0} [ver. {1}]", a.Location, a.GetName().Version);
                        }
                    }
                }
            }

            foreach (var pluginInfo in loadedPlugins)
            {
                var tp = pluginInfo.Plugin;
                if (tp != null)
                {
                    sb.AppendFormat("\n{0}", tp.DomainInfo);
                }
            }

            return sb.ToString();
#else
                return String.Empty;
#endif
        }
    }

    #endregion Properties

    #region Plugin Loading

    public static TcPlugin GetTcPlugin(string wrapperAssembly, PluginType pluginType)
    {
        pluginSettings = GetPluginSettings(wrapperAssembly);
#if TRACE
        writeTrace = Convert.ToBoolean(pluginSettings["writeTrace"]);
        TraceOut(string.Format(TraceMsg1, wrapperAssembly), TraceMsg2);
#endif
        TcPlugin tcPlugin;
        try
        {
            var pluginAssembly = pluginSettings["pluginAssembly"];
            if (string.IsNullOrEmpty(pluginAssembly))
            {
                pluginAssembly = Path.ChangeExtension(wrapperAssembly, ".dll");
            }

            var wrapperFolder = Path.GetDirectoryName(wrapperAssembly);
            if (!Path.IsPathRooted(pluginAssembly))
            {
                pluginAssembly = Path.Combine(wrapperFolder, pluginAssembly);
            }
#if TRACE
            TraceOut(pluginAssembly, TraceMsg3);
#endif
            var pluginFolder = Path.GetDirectoryName(pluginAssembly);
            pluginSettings["pluginFolder"] = pluginFolder;
            var pluginAssemblyName = Path.GetFileNameWithoutExtension(pluginAssembly);
            var title = pluginSettings["pluginTitle"] ?? pluginAssemblyName;
            AppDomain pluginDomain;
            var useSeparateDomain = !Convert.ToBoolean(pluginSettings["startInDefaultDomain"]);
            if (useSeparateDomain)
            {
                pluginDomain = CreatePluginAppDomain(pluginAssembly, title);
            }
            else
            {
                pluginDomain = MainDomain;
                MainDomain.AssemblyResolve += MainDomainResolveEventHandler;
#if TRACE
                TraceOut(null, "Load into Default AppDomain.");
#endif
            }

            var pluginClassName = pluginSettings["pluginClass"];
            // Create object implemented required TC plugin interface.
            tcPlugin = CreateTcPluginStub(pluginDomain, pluginAssembly, pluginType, null, ref pluginClassName);
            if (tcPlugin == null)
            {
                if (string.IsNullOrEmpty(pluginClassName))
                {
                    throw new InvalidOperationException(ErrorMsg2);
                }
            }
            else
            {
                tcPlugin.MainDomain = MainDomain;
#if TRACE
                TraceOut(
                    string.Format(TraceMsg6, tcPlugin.Title, pluginClassName),
                    string.Format(TraceMsg7, TcUtils.PluginNames[pluginType]));
#endif
            }

            // File System plugins only - try to load content plugin associated with current FS plugin
            if (pluginType == PluginType.FileSystem)
            {
                var contentAssembly = pluginSettings.ContainsKey("contentAssembly") ? pluginSettings["contentAssembly"] : pluginAssembly;
                if (!string.IsNullOrEmpty(contentAssembly))
                {
                    if (!Path.IsPathRooted(contentAssembly))
                    {
                        contentAssembly = Path.Combine(wrapperFolder, contentAssembly);
                    }

                    var contentClassName = pluginSettings["contentClass"];
#if TRACE
                    TraceOut(contentAssembly, TraceMsg8);
#endif

                    // Create object implemented Content plugin interface.
                    // Full FS plugin class name (with assembly name) 
                    // is passed as "masterClassName" parameter.
                    var fullPluginClassName = pluginClassName + ", " + pluginAssembly;
                    try
                    {
                        var cntPlugin = (ContentPlugin)CreateTcPluginStub(
                            pluginDomain,
                            contentAssembly,
                            PluginType.Content,
                            fullPluginClassName,
                            ref contentClassName);
                        var fsPlugin = (FsPlugin)tcPlugin;
                        if (fsPlugin != null)
                        {
                            if (cntPlugin == null)
                            {
                                if (string.IsNullOrEmpty(contentClassName))
                                {
                                    // No implementation for Content plugin interface
#if TRACE
                                    TraceOut(null, TraceMsg9);
#endif
                                }
                                else if (pluginClassName.Equals(contentClassName))
                                {
                                    // Content plugin interface is implemented in found FS plugin class
                                    try
                                    {
                                        cntPlugin = (ContentPlugin)tcPlugin;
                                        cntPlugin.PluginNumber = fsPlugin.PluginNumber;
                                        fsPlugin.ContentPlgn = cntPlugin;
#if TRACE
                                        TraceOut(null, TraceMsg10);
#endif
                                    }
                                    catch (InvalidCastException)
                                    {
                                    }
                                }
                            }
                            else
                            {
                                cntPlugin.PluginNumber = fsPlugin.PluginNumber;
                                fsPlugin.ContentPlgn = cntPlugin;
#if TRACE
                                TraceOut(string.Format(TraceMsg11, contentClassName), TraceMsg12);
#endif
                            }
                        }
                    }
                    catch (Exception ex)
                    {
#if TRACE
                        TraceOut(string.Format(TraceMsg13, contentClassName, ex.Message), string.Empty);
#endif
                    }
                }
            }

            if (useSeparateDomain)
            {
                tcPlugin.TcPluginEventHandler += HandleTcPluginEvent;
            }
            else
            {
                tcPlugin.TcPluginEventHandler += TcCallback.HandleTcPluginEvent;
            }

            tcPlugin.WrapperFileName = wrapperAssembly;
            var loadingInfo = FindPluginLoadingInfoByWrapperFileName(wrapperAssembly);
            if (loadingInfo == null)
            {
                loadedPlugins.Add(new TcPluginLoadingInfo(wrapperAssembly, tcPlugin, pluginDomain));
            }
            else
            {
                if (loadingInfo.LifetimeStatus == PluginLifetimeStatus.PluginUnloaded)
                {
                    tcPlugin.PluginNumber = loadingInfo.PluginNumber;
                    tcPlugin.CreatePassword(loadingInfo.CryptoNumber, loadingInfo.CryptoFlags);

                    loadingInfo.LifetimeStatus = PluginLifetimeStatus.Active;
                    loadingInfo.Plugin = tcPlugin;
                    loadingInfo.Domain = pluginDomain;
#if TRACE
                    TraceOut(string.Format(TraceMsg14, tcPlugin.Title), string.Empty);
#endif
                }
            }
        }
        finally
        {
#if TRACE
            writeTrace = false;
#endif
        }

        return tcPlugin;
    }

    // Creates new application domain for TC plugin.
    private static AppDomain CreatePluginAppDomain(string pluginAssembly, string title)
    {
        MainDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyEventHandler;
        var domainInfo = new AppDomainSetup
        {
            ApplicationBase = Path.GetDirectoryName(pluginAssembly),
            ConfigurationFile = pluginAssembly + ".config"
        };

        var domain = AppDomain.CreateDomain(title, null, domainInfo);
#if TRACE
        TraceOut(string.Format(TraceMsg4, domain.Id, domain.FriendlyName, domain.BaseDirectory), TraceMsg5);
#endif
        return domain;
    }

    // Creates remote object in plugin AppDomain via shared interface
    // to avoid loading the plugin library into main AppDomain.
    private static TcPlugin CreateTcPluginStub(
        AppDomain domain,
        string pluginAssembly,
        PluginType pluginType,
        string masterClassName,
        ref string pluginClassName)
    {
        var classNameToFind = pluginClassName;
        // Define class in plugin assembly implemented required TC plugin interface
        pluginClassName = GetPluginClassName(pluginAssembly, pluginType, pluginClassName);
        if (string.IsNullOrEmpty(pluginClassName))
        {
            if (!string.IsNullOrEmpty(classNameToFind))
            {
                throw new InvalidOperationException(string.Format(ErrorMsg5, classNameToFind));
            }

            return null;
        }

        // We expect the stub object for master class is already created
        var fullPluginClassName = pluginClassName + ", " + pluginAssembly;
        if (fullPluginClassName.Equals(masterClassName))
        {
            return null;
        }

        // Create remote object in plugin AppDomain.
        // Plugin constructor with "pluginSettings" parameter is called 
        var tpObj = domain.CreateInstanceAndUnwrap(
            Path.GetFileNameWithoutExtension(pluginAssembly),
            pluginClassName,
            false,
            BindingFlags.Default,
            null,
            new object[] { pluginSettings },
            null,
            null);
        if (tpObj == null)
        {
            throw new InvalidOperationException(string.Format(ErrorMsg1, pluginClassName));
        }

        return (TcPlugin)tpObj;
    }

    // Returns name for class in plugin assembly implemented required TC Plugin interface.
    private static string GetPluginClassName(string pluginDll, PluginType pluginType, string pluginClassName)
    {
        // We don't want to load Plugin assembly into main AppDomain (even as reflection only assembly).
        // So we analize types inside Plugin assembly in temporary AppDomain
        var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
        domain.ReflectionOnlyAssemblyResolve += ReflectionOnlyEventHandler;
        domain.SetData("pluginDll", pluginDll);
        domain.SetData("pluginType", pluginType);
        domain.SetData("pluginClassName", pluginClassName);
        domain.DoCallBack(GetPluginClassNameCallback);
        var result = (string)domain.GetData("pluginClassName");
        AppDomain.Unload(domain);
        return result;
    }

    private static void GetPluginClassNameCallback()
    {
        var domain = AppDomain.CurrentDomain;
        var pluginDll = (string)domain.GetData("pluginDll");
        var pluginType = (PluginType)domain.GetData("pluginType");
        var pluginClassName = (string)domain.GetData("pluginClassName");
        var type = GetPluginClass(pluginDll, pluginType, pluginClassName);
        var result = type == null ? null : type.FullName;
        domain.SetData("pluginClassName", result);
    }

    // Returns class in plugin assembly implemented interface for required TC Plugin.
    // Plugin assembly can contain several classes implementing the same interface.
    // You can specify desired class name in "pluginClassName" parameter,
    // or first class implementing required interface will be returned.
    public static Type GetPluginClass(string assemblyPath, PluginType pluginType, string pluginClassName)
    {
        if (pluginType == PluginType.Unknown)
        {
            return null;
        }

        var assembly = AssemblyReflectionOnlyLoadFrom(assemblyPath);
        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.GetInterface(TcUtils.PluginInterfaces[pluginType]) != null
                && (string.IsNullOrEmpty(pluginClassName) || pluginClassName.Equals(type.Name)))
            {
                return type;
            }
        }

        return null;
    }

    public static IListerHandlerBuilder GetListerHandlerBuilder(string wrapperAssembly)
    {
        try
        {
            var loadingInfo = FindPluginLoadingInfoByWrapperFileName(wrapperAssembly);
            if (loadingInfo != null)
            {
                var plugin = loadingInfo.Plugin;
                if (plugin != null && plugin is ListerPlugin)
                {
                    IListerHandlerBuilder lhBuilder = null;
                    var guiType = plugin.Settings["guiType"];
                    if (string.IsNullOrEmpty(guiType)
                        || guiType.Equals(ListerPlugin.WFListerHandlerBuilderName))
                    {
                        lhBuilder = new WFListerHandlerBuilder();
                    }
                    else if (guiType.Equals(ListerPlugin.WPFListerHandlerBuilderName))
                    {
                        lhBuilder = new WPFListerHandlerBuilder();
                    }

                    if (lhBuilder != null)
                    {
                        lhBuilder.Plugin = (ListerPlugin)plugin;
                        return lhBuilder;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    public static Assembly AssemblyLoad(string assemblyName) => Assembly.Load(assemblyName);

    public static Assembly AssemblyLoadFrom(string assemblyPath) => Assembly.LoadFrom(assemblyPath);

    public static Assembly AssemblyReflectionOnlyLoadFrom(string assemblyPath) => Assembly.ReflectionOnlyLoadFrom(assemblyPath);

    private static StringDictionary GetPluginSettings(string wrapperAssembly)
    {
        var result = new StringDictionary();
        var config = ConfigurationManager.OpenExeConfiguration(wrapperAssembly);
        var appSettings = config.AppSettings;
        if (appSettings != null)
        {
            foreach (var key in appSettings.Settings.AllKeys)
            {
                result.Add(key, appSettings.Settings[key].Value);
            }
        }

        return result;
    }

    private static Assembly ReflectionOnlyEventHandler(object sender, ResolveEventArgs args)
    {
        try
        {
            return Assembly.ReflectionOnlyLoad(args.Name);
        }
        catch (FileNotFoundException)
        {
            return Assembly.ReflectionOnlyLoadFrom(
                Path.Combine(
                    Path.GetDirectoryName(((AppDomain)sender).GetData("pluginDll").ToString()),
                    args.Name.Split(',')[0] + ".dll"));
        }
    }

    private static Assembly MainDomainResolveEventHandler(object sender, ResolveEventArgs args) =>
        AssemblyLoadFrom(Path.Combine(pluginSettings["pluginFolder"], args.Name.Split(',')[0] + ".dll"));

    #endregion Plugin Loading

    #region Other Methods

    internal static TcPlugin GetTcPluginById(string id)
    {
        foreach (var pluginLoadingInfo in loadedPlugins)
        {
            if (pluginLoadingInfo.Plugin.PluginId.Equals(id))
            {
                return pluginLoadingInfo.Plugin;
            }
        }

        return null;
    }

    public static AppDomain GetPluginDomainByFileName(string pluginFile)
    {
        foreach (var pluginLoadingInfo in loadedPlugins)
        {
            if (pluginLoadingInfo.WrapperFileName.Equals(pluginFile))
            {
                return pluginLoadingInfo.Domain;
            }
        }

        return null;
    }

    public static TcPluginLoadingInfo FindPluginLoadingInfoByPluginId(string pluginId)
    {
        foreach (var pluginLoadingInfo in loadedPlugins)
        {
            if (pluginLoadingInfo.Plugin.PluginId.Equals(pluginId))
            {
                return pluginLoadingInfo;
            }
        }

        return null;
    }

    public static TcPluginLoadingInfo FindPluginLoadingInfoByWrapperFileName(string pluginWrapperFileName)
    {
        foreach (var pluginLoadingInfo in loadedPlugins)
        {
            if (pluginLoadingInfo.WrapperFileName.Equals(pluginWrapperFileName))
            {
                return pluginLoadingInfo;
            }
        }

        return null;
    }

    public static TcPluginLoadingInfo FindPluginLoadingInfoByPluginNumber(int pluginNumber)
    {
        foreach (var pluginLoadingInfo in loadedPlugins)
        {
            if (pluginLoadingInfo.PluginNumber == pluginNumber)
            {
                return pluginLoadingInfo;
            }
        }

        return null;
    }

    public static PluginLifetimeStatus CheckPluginLifetimeStatus(Exception ex)
    {
        var pluginDisconnected =
            ex is RemotingException && ex.Message.EndsWith(LifetimeExpiredMsgEnd);
        if (!pluginDisconnected)
        {
            return PluginLifetimeStatus.Active;
        }

        var pluginWrapperFile = Assembly.GetCallingAssembly().Location;
        var loadingInfo = FindPluginLoadingInfoByWrapperFileName(pluginWrapperFile);
        if (loadingInfo == null)
        {
            return PluginLifetimeStatus.NotLoaded;
        }

        var stackTrace = new StackTrace(true);
#if TRACE
        var category = loadingInfo.PluginNumber.ToString(CultureInfo.InvariantCulture);
        string text;
#endif
        var stackFrames = stackTrace.GetFrames();
        if (stackFrames != null)
        {
            foreach (var sf in stackFrames)
            {
                var declaringType = sf.GetMethod().DeclaringType;
                if (declaringType != null && declaringType.FullName.StartsWith("OY.TotalCommander.WfxWrapper") &&
                    !sf.GetMethod().Name.Equals("ProcessException"))
                {
#if TRACE
                    text = string.Format(TraceMsg15, declaringType.FullName.Substring(29), sf.GetMethod().Name);
                    TraceError(text, category);
#endif
                    break;
                }
            }
        }

        // Plugin remote object has expired, try to unload plugin AppDomain
        loadingInfo.LifetimeStatus = PluginLifetimeStatus.Expired;
        if (loadingInfo.UnloadExpired)
        {
            var pluginDomain = loadingInfo.Domain;
            try
            {
                AppDomain.Unload(pluginDomain);
#if TRACE
                text = "Plugin Lifetime Expired - Plugin \"" + pluginWrapperFile + "\" was disconnected from TC.";
                TraceError(text, category);
#endif
                MessageBox.Show(
                    "Plugin " + Path.GetFileNameWithoutExtension(pluginWrapperFile)
                              + " has expired and was disconnected From TC.",
                    "Plugin disconnected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                OpenTcPluginHome();
                loadingInfo.LifetimeStatus = PluginLifetimeStatus.PluginUnloaded;
            }
            catch (Exception e)
            {
#if TRACE
                text = "Unload ERROR: " + e.Message;
                TraceError(text, category);
#endif
            }
        }
        else
        {
            MessageBox.Show(
                "Plugin " + Path.GetFileNameWithoutExtension(pluginWrapperFile) + " has expired.",
                "Plugin expired",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        return loadingInfo.LifetimeStatus;
    }

    public static void ProcessException(
        TcPlugin plugin,
        bool pluginDisconnected,
        string callSignature,
        Exception ex)
    {
#if TRACE
        var pluginTitle =
            plugin == null || pluginDisconnected ? "NULL" : plugin.TraceTitle;
        TcTrace.TraceError(string.Format("{0}: {1}", callSignature, ex.Message), pluginTitle);
#endif
        if (ex is MethodNotSupportedException)
        {
            if (!((MethodNotSupportedException)ex).Mandatory)
            {
                return;
            }
        }

        if (plugin == null || pluginDisconnected || plugin.ShowErrorDialog)
        {
            ErrorDialog.Show(callSignature, ex);
        }

        // TODO: add - unload plugin AppDomain on crytical error (configurable)
    }

    public static void FillLoadingInfo(TcPlugin plugin)
    {
        var loadingInfo =
            FindPluginLoadingInfoByWrapperFileName(plugin.WrapperFileName);
        if (loadingInfo != null)
        {
            loadingInfo.PluginNumber = plugin.PluginNumber;
            if (plugin.Password != null)
            {
                loadingInfo.CryptoNumber = plugin.Password.GetCryptoNumber();
                loadingInfo.CryptoFlags = plugin.Password.GetFlags();
            }

            if (plugin is FsPlugin)
            {
                loadingInfo.UnloadExpired = ((FsPlugin)plugin).UnloadExpired;
            }
        }
    }

    public static void SetTcMainWindowHandle(IntPtr handle)
    {
        if (tcMainWindowHandle == IntPtr.Zero)
        {
            tcMainWindowHandle = handle;
        }
    }

    private const int CmOpenNetwork = 2125;

    private static void OpenTcPluginHome()
    {
        if (tcMainWindowHandle != IntPtr.Zero)
        {
            TcWindow.SendMessage(tcMainWindowHandle, CmOpenNetwork);
            Thread.Sleep(500);
        }
    }

#if TRACE
    private static void TraceOut(string text, string category)
    {
        if (writeTrace)
        {
            if (category.Equals("Start"))
            {
                TcTrace.TraceDelimiter();
            }

            TcTrace.TraceOut(TraceLevel.Warning, text, category);
        }
    }

    private static void TraceError(string text, string category) => TcTrace.TraceOut(TraceLevel.Error, text, category);
#endif

    #endregion Other Methods
}
