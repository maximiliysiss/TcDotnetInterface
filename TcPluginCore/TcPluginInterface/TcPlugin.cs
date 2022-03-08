using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Lifetime;
using System.Security.Permissions;
using System.Text;
using System.Threading;
#if TRACE
#endif

namespace TcPluginInterface
{
    [Serializable]
    public class TcPlugin : MarshalByRefObject
    {
        #region MarshalByRefObject - Lifetime initialization

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            if (lease is { CurrentState: LeaseState.Initial })
            {
                // By default we set infinite lifetime for each created plugin (initialLifeTime = 0)
                lease.InitialLeaseTime = TimeSpan.Zero;
            }

            return lease;
        }

        #endregion

        #region Variables

        private static readonly string _tcFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        private int _mainThreadId;

        #endregion Variables

        #region Properties

        public string DataBufferName { get; private set; }

        public AppDomain MainDomain { get; set; }

        public PluginDefaultParams DefaultParams { get; set; }

        public string DomainInfo
        {
            get
            {
#if TRACE
                var sb = new StringBuilder();
                var domain = AppDomain.CurrentDomain;
                sb.Append("-----------");
                sb.AppendFormat("\nPlugin: {0}", Title);
                if (domain.Equals(MainDomain))
                    return sb.ToString();

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

                return sb.ToString();
#else
                return String.Empty;
#endif
            }
        }

        protected bool IsBackgroundThread => Thread.CurrentThread.ManagedThreadId != _mainThreadId;

        public TcPlugin MasterPlugin { get; set; }

        public PluginPassword Password { get; protected set; }

        protected string PluginFolder { get; private set; }

        public string PluginId { get; private set; }

        public int PluginNumber { get; set; }

        public StringDictionary Settings { get; private set; }

        public bool ShowErrorDialog { get; set; }

        public string Title { get; set; }

        public virtual string TraceTitle => MasterPlugin == null ? Title : MasterPlugin.TraceTitle;

        public string WrapperFileName { get; set; }

        public bool WriteTrace { get; private set; }

        #endregion Properties

        #region Constructors

        public TcPlugin() => PluginInit(null);

        public TcPlugin(StringDictionary pluginSettings) => PluginInit(pluginSettings);

        private void PluginInit(StringDictionary pluginSettings)
        {
            PluginId = Guid.NewGuid().ToString();
            DataBufferName = Guid.NewGuid().ToString();
            PluginNumber = -1;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            if (pluginSettings != null)
            {
                Settings = pluginSettings;
                PluginFolder = pluginSettings["pluginFolder"];
                Title = pluginSettings["pluginTitle"];
                ShowErrorDialog = !Convert.ToBoolean(pluginSettings["hideErrorDialog"]);
                WriteTrace = Convert.ToBoolean(pluginSettings["writeTrace"]);
            }
        }

        #endregion Constructors

        #region Plugin Event Handler

        public event EventHandler<PluginEventArgs> TcPluginEventHandler;

        public virtual int OnTcPluginEvent(PluginEventArgs e)
        {
            var handler = TcPluginEventHandler;
            if (handler == null)
                return 0;

            handler(this, e);
            return e.Result;
        }

        #endregion Plugin Event Handler

        #region Trace Handler

        protected void TraceProc(TraceLevel level, string text)
        {
#if TRACE
            PluginDomainTraceHandler(this, new TraceEventArgs(level, text));
#endif
        }

#if TRACE
        private const string TraceCallbackPluginId = "TraceCallbackPluginId";
        private const string TraceCallbackEventArg = "TraceCallbackEventArg";

        protected static void PluginDomainTraceHandler(object sender, TraceEventArgs e)
        {
            var tp = sender as TcPlugin;
            if (tp == null)
            {
                return;
            }

            var mainDomain = tp.MainDomain;
            mainDomain.SetData(TraceCallbackPluginId, tp.TraceTitle);
            mainDomain.SetData(TraceCallbackEventArg, e);
            try
            {
                mainDomain.DoCallBack(MainDomainTraceHandler);
            }
            finally
            {
                mainDomain.SetData(TraceCallbackEventArg, null);
                mainDomain.SetData(TraceCallbackPluginId, null);
            }
        }

        public static void MainDomainTraceHandler()
        {
            var category = (string)AppDomain.CurrentDomain.GetData(TraceCallbackPluginId);
            var e = (TraceEventArgs)AppDomain.CurrentDomain.GetData(TraceCallbackEventArg);
            TcTrace.TraceOut(e.Level, e.Text, category);
        }
#endif

        #endregion Trace Handler

        #region Other Methods

        public virtual void OnTcTrace(TraceLevel level, string text)
        {
        }

        public virtual void CreatePassword(int cryptoNumber, int flags) => Password = null;

        protected void SetPluginFolder(string folderKey, string defaultFolder)
        {
            var folderName = Settings.ContainsKey(folderKey) ? Settings[folderKey] : defaultFolder;
            if (string.IsNullOrEmpty(folderName))
                return;

            folderName = folderName.Replace("%TC%", _tcFolder).Replace("%PLUGIN%", PluginFolder);
            Settings[folderKey] = folderName;
        }

        #endregion Other Methods
    }
}
