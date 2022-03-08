using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using TcPluginInterface;
using TcPluginTools;

namespace WrapperBuilder;

internal sealed class Program
{
    #region Main

    private static void Main(string[] args)
    {
        try
        {
            ParseArgs(args);
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
            WriteLine($"Wrapper Assembly '{_wrapperAssemblyFile}'");
            var exportedMethods = LoadExportedList(_wrapperAssemblyFile);
            WriteLine($"Plugin type : {TcUtils.PluginNames[_pluginType]}");
            if (exportedMethods.Count <= 0)
            {
                throw new Exception(ErrorMsg6);
            }

            WriteLine($"- {exportedMethods.Count} Methods to export");
            SetOutputNames();
            var excludedMethods = new List<string>();
            if (!string.IsNullOrEmpty(_pluginAssemblyFile))
            {
                excludedMethods = LoadExcludedList();
                if (_pluginType.Equals(PluginType.FileSystem))
                {
                    if (!string.IsNullOrEmpty(_iconFileName) && File.Exists(_iconFileName))
                    {
                        WriteLine($"Plugin Icon added: {_iconFileName}");
                        _iconFileName = _iconFileName.Replace("\\", "\\\\");
                    }
                    else if (_iconFromPluginAssembly)
                    {
                        _iconFileName = GetTmpIconFileName(_pluginAssemblyFile);
                    }
                }
            }

            var sourcePath = Disassemble();
            WriteLine($"Disassembled to '{sourcePath}'");
            var sourceOutPath = $@"{_workDir}\output.il";
            ProcessSource(sourcePath, sourceOutPath, exportedMethods, excludedMethods);
            WriteLine($"Processed to '{sourceOutPath}'");

            if (_x32Flag)
            {
                Assemble(false);
            }

            if (_x64Flag)
            {
                Assemble(true);
            }

            if (!_outputWrapperFolder.Equals(Path.GetDirectoryName(_pluginAssemblyFile)))
            {
                File.Copy(
                    _pluginAssemblyFile,
                    Path.Combine(_outputWrapperFolder, Path.GetFileName(_pluginAssemblyFile)),
                    true);
            }

            if (_createInstZip)
            {
                CreateInstallationZip();
            }

            if (_clearWorkDir)
            {
                Directory.Delete(_workDir, true);
            }

            if (_pause)
            {
                Console.Read();
            }
        }
        catch (Exception ex)
        {
            WriteLine($"ERROR: {ex.Message}", false);
            if (_pause)
            {
                Console.Read();
            }

            Environment.Exit(1);
        }
    }

    #endregion Main

    #region Constants

    private const string PluginInterfacePropertyName = "Plugin";
    private static readonly NameValueCollection _appSettings = ConfigurationManager.AppSettings;

    // Error Messages
    private const string ErrorMsg0 =
        "Use only one argument for wrapper assembly in parameters (/wcx | /wdx | /wfx | /wlx | /w=<wrapperDLL>).";

    private const string ErrorMsg1 = "Wrapper assembly '{0}' is empty or does not exist!";
    private const string ErrorMsg2 = "Cannot locate IL Assembler ilasm.exe!";
    private const string ErrorMsg3 = "Cannot locate IL Disassembler ildasm.exe!";
    private const string ErrorMsg4 = "Wrapper assembly '{0}' contains more than one TC plugin interface: ({1}, {2})!";
    private const string ErrorMsg5 = "Wrapper assembly '{0}' contains no TC plugin interface!";
    private const string ErrorMsg6 = "No methods to export, check your wrapper assembly!";
    private const string ErrorMsg7 = "{0} plugin is not implemented in '{1}'";
    private const string ErrorMsg8 = "ilasm.exe has failed assembling generated source!";
    private const string ErrorMsg9 = "ildasm.exe has failed disassembling {0}!";

    private const string ErrorMsg10 =
        "Use only one processor architecture flag in parameters (/x32 | /x64), or skip them all to create both 32- and 64-bit plugins.";

    private const string ErrorMsg11 = "Plugin assembly '{0}' is empty or does not exist!";

//        private const string ErrorMsg12 = "Cannot locate ZIP Archiver '{0}' - add path to settings!";
    private const string WarningMsg1 = "Cannot locate Resource Compiler rc.exe.";
    private const string WarningMsg2 = "File '{0}' - Resource Compiler ERROR.";
    private const string WarningMsg3 = "    WARNING!!! Type '{0}' - mandatory methods not implemented : {1}";

    private static readonly Dictionary<PluginType, string> _pluginExtensions =
        new()
        {
            { PluginType.Content, "wdx" },
            { PluginType.FileSystem, "wfx" },
            { PluginType.Lister, "wlx" },
            { PluginType.Packer, "wcx" },
            { PluginType.QuickSearch, "dll" }
        };

    private static readonly Dictionary<PluginType, string> _pluginMethodPrefixes =
        new()
        {
            { PluginType.Content, "Content" },
            { PluginType.FileSystem, "Fs" },
            { PluginType.Lister, "List" },
            { PluginType.Packer, null },
            { PluginType.QuickSearch, null }
        };

    // Mandatory plugin methods, parts of .NET plugin interface.
    // They must be implemented in plugin assembly.
    private static readonly Dictionary<PluginType, string[]> _pluginMandatoryMethods =
        new()
        {
            {
                PluginType.Content,
                new[]
                {
                    "GetSupportedField",
                    "GetValue"
                }
            },
            {
                PluginType.FileSystem,
                new[]
                {
                    "FindFirst",
                    "FindNext"
                }
            },
            {
                PluginType.Lister,
                new[]
                {
                    "Load"
                }
            },
            {
                PluginType.Packer,
                new[]
                {
                    "OpenArchive",
                    "ReadHeader",
                    "ProcessFile",
                    "CloseArchive"
                }
            },
            {
                PluginType.QuickSearch,
                new[]
                {
                    "MatchFile",
                    "MatchGetSetOptions"
                }
            }
        };

    // Optional plugin methods - can be omitted in plugin assembly.
    // We have to exclude their calls from wrapper because of TC plugin requirements:
    // "(must NOT be implemented if unsupported!)" - from TC plugin help.)
    private static readonly Dictionary<PluginType, string[]> _pluginOptionalMethods =
        new()
        {
            {
                PluginType.Content,
                new[]
                {
                    "StopGetValue",
                    "GetDefaultSortOrder",
                    "PluginUnloading",
                    "GetSupportedFieldFlags",
                    "SetValue",
                    "GetDefaultView",
                    "EditValue",
                    "SendStateInformation",
                    "CompareFiles"
                }
            },
            {
                PluginType.FileSystem,
                new[]
                {
                    "GetFile",
                    "PutFile",
                    "RenMovFile",
                    "DeleteFile",
                    "RemoveDir",
                    "MkDir",
                    "ExecuteFile",
                    "SetAttr",
                    "SetTime",
                    "Disconnect",
                    "ExtractCustomIcon",
                    "GetPreviewBitmap",
                    "GetLocalName"
                }
            },
            {
                PluginType.Lister,
                new[]
                {
                    "LoadNext",
                    "CloseWindow",
                    "SearchText",
                    "SendCommand",
                    "Print",
                    "NotificationReceived",
                    "GetPreviewBitmap",
                    "SearchDialog"
                }
            },
            {
                PluginType.Packer,
                new[]
                {
                    "PackFiles",
                    "DeleteFiles",
                    "ConfigurePacker",
                    "StartMemPack",
                    "PackToMem",
                    "DoneMemPack",
                    "CanYouHandleThisFile"
                }
            },
            {
                PluginType.QuickSearch,
                new string[] { }
            }
        };

    // Other plugin methods:
    //   1. TC plugin methods implemented in plugin wrapper only and are not parts of .NET plugin interface, or
    //   2. Methods implemented in parent plugin class and can be omitted in plugin assembly.
    // We DON'T have to exclude them from plugin wrapper.
    private static readonly Dictionary<PluginType, string[]> _pluginOtherMethods =
        new()
        {
            {
                PluginType.Content,
                new[]
                {
                    "GetDetectString"
                    //"SetDefaultParams" - commented to prevent excluding "FsSetDefaultParams" method for FS plugin without Content features
                }
            },
            {
                PluginType.FileSystem,
                new[]
                {
                    "Init",
                    "FindClose",
                    "SetCryptCallback",
                    "GetDefRootName",
                    "SetDefaultParams",
                    "GetBackgroundFlags",
                    "LinksToLocalFiles",
                    "StatusInfo"
                }
            },
            {
                PluginType.Lister,
                new[]
                {
                    "GetDetectString",
                    "SetDefaultParams"
                }
            },
            {
                PluginType.Packer,
                new[]
                {
                    "SetChangeVolProc",
                    "SetProcessDataProc",
                    "GetPackerCaps",
                    "PackSetDefaultParams",
                    "PkSetCryptCallback",
                    "GetBackgroundFlags"
                }
            },
            {
                PluginType.QuickSearch,
                new string[] { }
            }
        };

    private static readonly string[] _resourceTemplate =
    {
        "1 VERSIONINFO",
        "FILEVERSION {FileVersion}",
        "PRODUCTVERSION {FileVersion}",
        "FILEOS 0x4",
        "FILETYPE 0x2",
        "{",
        "BLOCK \"StringFileInfo\"",
        "{",
        "  BLOCK \"000004b0\"",
        "  {",
        "{Values}",
        "  }",
        "}",
        "BLOCK \"VarFileInfo\"",
        "{",
        "  VALUE \"Translation\", 0x0000 0x04B0",
        "}",
        "}",
        "",
        "ICON_1 ICON \"{IconFile}\""
    };

    private static readonly string[] _configTemplate =
    {
        "<?xml version=\"1.0\" encoding=\"utf-8\" ?>",
        "<configuration>",
        "  <appSettings>",
        "    <add key=\"pluginAssembly\" value=\"{PluginAssembly}\"/>",
        "    <!-- add key=\"pluginTitle\" value=\"???\"/> -->",
        "    <add key=\"writeStatusInfo\" value=\"true\"/>",
        "    <add key=\"writeTrace\" value=\"true\"/>",
        "",
        "    <!-- write your plugin settings here -->",
        "  </appSettings>",
        "</configuration>"
    };

    private const string DefaultDescription = "...Put your description here...";

    private static readonly string[] _pluginsTemplate =
    {
        "[plugininstall]",
        "description={Description}",
        "type={PluginType}",
        "file={PluginFile}",
        "defaultdir={DefaultDir}",
        "defaultextension=???"
    };

    private static readonly string[] _usageInfo =
    {
        "Builds wrapper DLL(s) for Total Commander plugin written in .NET",
        "",
        "Usage: ",
        "  WrapperBuilder (/wcx | /wdx | /wfx | /wlx | /w=<wrapperDLL>) ",
        "                 /p=<pluginDLL> [/c=<contentDLL>] [/o=<outputName>] ",
        "                 [/i=<iconFile> | /ipa] [/v] [/release] ([/x32] | [/x64])",
        "                 [/a=<assemblerPath>] [/d=<disassemblerPath>] [/r=<rcPath>]",
        "",
        "  /wcx /wdx /wfx /wlx /qs  Use one of standard plugin wrapper templates",
        "                           located in the program folder: ",
        "                             /wcx - WcxWrapper.dll for Packer plugin, or",
        "                             /wdx - WdxWrapper.dll for Content plugin, or",
        "                             /wfx - WfxWrapper.dll for File System plugin, or",
        "                             /wlx - WlxWrapper.dll for Lister plugin, or",
        "                             /qs  - QSWrapper.dll  for QuickSearch plugin",
        "  /w=<wrapperDLL>        Use your own wrapper template assembly.",
        "  /p=<pluginDLL>         Assembly implementing TC plugin interface.",
        "                         If some plugin interface function is not implemented",
        "                         here, it is excluded from wrapper.",
        "  /c=<contentDLL>        Assembly implementing TC Content interface.",
        "                         Used with File System wrapper only, if FS and Content",
        "                         interfaces are implemented in separate DLLs.",
        "  /o=<outputName>        Output wrapper file name (no path, no extension).",
        "                         If value is empty, plugin assembly file name is used.",
        "  /i=<iconFile>          Adds icon to wrapper assembly from <icon File>.",
        "  /ipa                   Adds icon to wrapper assembly extracting it from the",
        "                         plugin assembly.",
        "                         /i or /ipa flags are used for FS wrapper only.",
        "  /release               Adds optimizaton to output assembly.",
        "                         (Equals to ilasm.exe '/optimize' key)",
        "  /x32                   Creates only 32-bit (PE32) plugin wrapper.",
        "  /x64                   Creates only 64-bit (PE32+) plugin wrapper",
        "                         for 64-bit AMD processor as the target processor.",
        "                         (Equals to ilasm.exe '/pe64 /x64' key set)",
        "         If both /x32 and /x64 flags are skipped, ",
        "         both 32- and 64-bit plugin wrappers will be created.",
        "  /v                     Verbose mode; outputs log to console.",
        "  /a=<assemblerPath>     Specifies path to IL Assembler (ilasm.exe).",
        "                         If not set, path loaded from configuration file",
        "                         WrapperBuilder.exe.config, key='assemblerPath'.",
        "  /d=<disassemblerPath>  Specifies path to IL Disassembler (ildasm.exe).",
        "                         If not set, path loaded from configuration file",
        "                         WrapperBuilder.exe.config, key='disassemblerPath'.",
        "  /r=<rcPath>            Specifies path to Resource Compiler (rc.exe).",
        "                         If not set, path loaded from configuration file",
        "                         WrapperBuilder.exe.config, key='rcPath'.",
        "  /z=<zipPath>           Specifies path to ZIP archiver (usually zip.exe).",
        "                         If not set, path loaded from configuration file",
        "                         WrapperBuilder.exe.config, key='zipArchiver'.",
        "                         Is used to create Installation ZIP archive for plugin.",
        "                         If finally not set, Inst. Archive will not be created."
    };

    private const string ZipArchiverDefault = @"C:\TotalCmd\Arc\zip\zip.exe";

    #endregion Constants

    #region Variables

    private static readonly string _appFolder =
        Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

    private static readonly string _dllExportAttributeTypeName = typeof(DllExportAttribute).FullName;
    private static readonly List<string> _ilasmArgs = new();
    private static readonly string _workDir = GetWorkingDirectory();

    private static readonly bool _clearWorkDir = false;

    private static readonly bool _createConfiguration = true;

    private static readonly bool _createInstZip = true;

    private static string _assemblerPath;
    private static string _contentAssemblyFile;
    private static string _disassemblerPath;
    private static string _rcPath;
    private static string _iconFileName;
    private static bool _iconFromPluginAssembly;
    private static string _outputWrapperFolder;
    private static string _outputWrapperName;
    private static string _pluginAssemblyFile;
    private static PluginType _pluginType = PluginType.Unknown;
    private static bool _verbose;
    private static string _wrapperAssemblyFile;
    private static string _wrapperAssemblyVersion;
    private static bool _x32Flag = true;
    private static bool _x64Flag = true;
    private static bool _pause;

    #endregion Variables

    #region Private Methods

    private static string Assemble(bool x64)
    {
        var outputFileName = _outputWrapperName;
        var outputWrapperExt = $".{_pluginExtensions[_pluginType]}";
        var outPath = $@"{_workDir}\{Path.GetFileName(_wrapperAssemblyFile)}";
        var resourcePath = $@"{_workDir}\{"input.res"}";
        var args = new StringBuilder();
        args.AppendFormat(@"""{0}\output.il"" /out:""{1}""", _workDir, outPath);
        if (Path.GetExtension(_wrapperAssemblyFile) == ".dll")
        {
            args.Append(" /dll");
        }

        if (File.Exists(resourcePath))
        {
            args.AppendFormat(@" /res:""{0}""", resourcePath);
        }

        if (x64)
        {
            _ilasmArgs.Add("/x64");
            _ilasmArgs.Add("/PE64");
            if (_pluginType == PluginType.QuickSearch)
            {
                outputFileName += "64";
            }
            else
            {
                outputWrapperExt += "64";
            }

            WriteLine("\n64-bit plugin wrapper\n=====================");
        }
        else
        {
            WriteLine("\n32-bit plugin wrapper\n=====================");
        }

        if (_ilasmArgs.Count > 0)
        {
            args.Append(" ").Append(string.Join(" ", _ilasmArgs.ToArray()));
        }

        var startInfo =
            new ProcessStartInfo(_assemblerPath, args.ToString())
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };
        var process = Process.Start(startInfo);
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new Exception(ErrorMsg8);
        }

        WriteLine($"  Assembled to '{outPath}'");
        var newPath = //Path.Combine(outputWrapperFolder, outputWrapperName + outputWrapperExt);
            outputFileName + outputWrapperExt;
        File.Delete(newPath);
        File.Copy(outPath, newPath, true);
        WriteLine($"  Wrapper assembly moved to '{newPath}'");
        if (_createConfiguration)
        {
            CreateConfigFile(newPath);
        }

        return outPath;
    }

    private static string Disassemble()
    {
        var sourcePath = $@"{_workDir}\input.il";
        var args = $@"""{_wrapperAssemblyFile}"" /out:""{sourcePath}""";
        var startInfo =
            new ProcessStartInfo(_disassemblerPath, args)
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };
        var process = Process.Start(startInfo);
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new Exception(string.Format(ErrorMsg9, _wrapperAssemblyFile));
        }

        try
        {
            var rcFile = _rcPath ?? Path.Combine(Path.GetDirectoryName(_disassemblerPath), "rc.exe");
            if (string.IsNullOrEmpty(rcFile) || !File.Exists(rcFile))
            {
                throw new Exception(WarningMsg1);
            }

            var resFileName = $"{_workDir}\\input.rc";
            FillResourceFile(resFileName);
            args = $"/v {resFileName}";
            startInfo =
                new ProcessStartInfo(rcFile, args)
                {
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            process = Process.Start(startInfo);
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception(string.Format(WarningMsg2, resFileName));
            }
        }
        catch (Exception ex)
        {
            WriteLine($"WARNING: {ex.Message}", false);
        }

        return sourcePath;
    }

    private static void FillResourceFile(string resFileName)
    {
        string fileVersion = null;
        string productVersion = null;
        var version = FileVersionInfo.GetVersionInfo(_pluginAssemblyFile); // wrapperAssemblyFile);
        var versionInfo = new StringBuilder(500);
        if (!string.IsNullOrEmpty(version.Comments))
        {
            versionInfo.AppendFormat("    VALUE \"Comments\", \"{0}\"\n", version.Comments);
        }

        if (!string.IsNullOrEmpty(version.CompanyName))
        {
            versionInfo.AppendFormat("    VALUE \"CompanyName\", \"{0}\"\n", version.CompanyName);
        }

        if (!string.IsNullOrEmpty(version.FileDescription))
        {
            versionInfo.AppendFormat("    VALUE \"FileDescription\", \"{0}\"\n", version.FileDescription);
        }

        //if (!String.IsNullOrEmpty(version.FileName))
        //    versionInfo.AppendFormat("    VALUE \"FileName\", \"{0}\"\n", version.FileName);
        if (!string.IsNullOrEmpty(version.FileVersion))
        {
            versionInfo.AppendFormat("    VALUE \"FileVersion\", \"{0}\"\n", version.FileVersion);
            fileVersion = version.FileVersion.Replace('.', ',');
        }

        if (!string.IsNullOrEmpty(version.InternalName))
        {
            versionInfo.AppendFormat("    VALUE \"InternalName\", \"{0}\"\n", version.InternalName);
        }

        if (!string.IsNullOrEmpty(version.LegalCopyright))
        {
            versionInfo.AppendFormat("    VALUE \"LegalCopyright\", \"{0}\"\n", version.LegalCopyright);
        }

        if (!string.IsNullOrEmpty(version.LegalTrademarks))
        {
            versionInfo.AppendFormat("    VALUE \"LegalTrademarks\", \"{0}\"\n", version.LegalTrademarks);
        }

        if (!string.IsNullOrEmpty(version.OriginalFilename))
        {
            versionInfo.AppendFormat("    VALUE \"OriginalFilename\", \"{0}\"\n", version.OriginalFilename);
        }

        if (!string.IsNullOrEmpty(version.ProductName))
        {
            versionInfo.AppendFormat("    VALUE \"ProductName\", \"{0}\"\n", version.ProductName);
        }

        if (!string.IsNullOrEmpty(version.ProductVersion))
        {
            versionInfo.AppendFormat("    VALUE \"ProductVersion\", \"{0}\"\n", version.ProductVersion);
            productVersion = version.ProductVersion.Replace('.', ',');
        }

        versionInfo.AppendFormat("    VALUE \"Assembly Version\", \"{0}\"\n", _wrapperAssemblyVersion);
        using (var output = new StreamWriter(resFileName, false, Encoding.Default))
        {
            foreach (var str in _resourceTemplate)
            {
                var outputStr = str
                    .Replace("{FileVersion}", fileVersion)
                    .Replace("{ProductVersion}", productVersion)
                    .Replace("{Values}", versionInfo.ToString());
                if (str.Contains("{IconFile}"))
                {
                    if (string.IsNullOrEmpty(_iconFileName))
                    {
                        continue;
                    }

                    // Add icon to resource file for wrapper
                    outputStr = outputStr.Replace("{IconFile}", _iconFileName);
                }

                output.WriteLine(outputStr);
            }
        }
    }

    private static void CreateConfigFile(string wrapperFileName)
    {
        var configFileName = $"{wrapperFileName}.config";
        if (File.Exists(configFileName))
        {
            return;
        }

        using (var output = new StreamWriter(configFileName, false, Encoding.Default))
        {
            foreach (var str in _configTemplate)
            {
                if (str.Contains("\"writeStatusInfo\"") && !_pluginType.Equals(PluginType.FileSystem))
                {
                    continue;
                }

                output.WriteLine(
                    str
                        .Replace("{PluginAssembly}", Path.GetFileName(_pluginAssemblyFile)));
            }
        }
    }

    private static void CreatePluginstFile(string iniFileName)
    {
        if (File.Exists(iniFileName))
        {
            return;
        }

        var version = FileVersionInfo.GetVersionInfo(_pluginAssemblyFile);
        var description = version.Comments ?? DefaultDescription;
        var wrapperName = Path.GetFileNameWithoutExtension(_outputWrapperName);
        using (var output = new StreamWriter(iniFileName, false, Encoding.Default))
        {
            foreach (var str in _pluginsTemplate)
            {
                if (str.StartsWith("defaultextension") && !_pluginType.Equals(PluginType.Packer))
                {
                    continue;
                }

                output.WriteLine(
                    str
                        .Replace("{PluginType}", _pluginExtensions[_pluginType])
                        .Replace("{PluginFile}", $"{wrapperName}.{_pluginExtensions[_pluginType]}")
                        .Replace("{Description}", description)
                        .Replace("{DefaultDir}", $"dotNet_{wrapperName}"));
            }
        }
    }

    private static void CreateInstallationZip()
    {
        if (_pluginType == PluginType.QuickSearch)
        {
            return;
        }

        WriteLine("\nInstallation archive\n====================");

        string zipArchiver = null;
        if (_appSettings != null)
        {
            zipArchiver = _appSettings["zipArchiver"];
        }

        if (string.IsNullOrEmpty(zipArchiver))
        {
            zipArchiver = ZipArchiverDefault;
        }

        if (!File.Exists(zipArchiver))
        {
            WriteLine("ZIP Archiver is not found - Installation Archive is not created.");
            return;
//                throw new Exception(String.Format(ErrorMsg12, zipArchiver));
        }

        var iniFile = Path.Combine(_outputWrapperFolder, "pluginst.inf");
        CreatePluginstFile(iniFile); //   ???
        if (File.Exists(iniFile))
        {
            var outFile = _outputWrapperName;
            var wrapperFile = $"{outFile}.{_pluginExtensions[_pluginType]}";
            var args = $"-j -q {outFile}.zip";
            if (_x32Flag)
            {
                args += $" {wrapperFile}";
                if (File.Exists($"{wrapperFile}.config"))
                {
                    args += $" {wrapperFile}.config";
                }
            }

            if (_x64Flag)
            {
                wrapperFile += "64";
                args += $" {wrapperFile}";
                if (File.Exists($"{wrapperFile}.config"))
                {
                    args += $" {wrapperFile}.config";
                }
            }

            args += $" {_pluginAssemblyFile}";
            args += $" {iniFile}";

            var startInfo = new ProcessStartInfo(zipArchiver, args)
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var process = Process.Start(startInfo);
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                WriteLine("  ERROR archive creating !!!");
            }
            else
            {
                WriteLine($"  Archive created: '{outFile}.zip'");
                File.Delete(iniFile);
            }
        }
    }

    private static List<string> GetExportedMethods(Assembly assembly)
    {
        var methods = new List<string>();
        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                var methodName = method.Name;
                foreach (var attr in CustomAttributeData.GetCustomAttributes(method))
                {
                    if (attr.Constructor.DeclaringType != null
                        && attr.Constructor.DeclaringType.FullName.Equals(_dllExportAttributeTypeName))
                    {
                        if (attr.NamedArguments != null)
                        {
                            foreach (var arg in attr.NamedArguments)
                            {
                                if (arg.MemberInfo.Name.Equals("EntryPoint"))
                                {
                                    methodName = (string)arg.TypedValue.Value;
                                }
                            }
                        }

                        methods.Add(methodName);
                    }
                }
            }
        }

        return methods;
    }

    private static string GetTmpIconFileName(string pluginAssembly)
    {
        // Try to extract icon from plugin assembly to temporary file
        var iconFile = "icon_tmp.ico";
        try
        {
            var icon = Icon.ExtractAssociatedIcon(pluginAssembly);
            if (icon != null)
            {
                using (Stream s = new FileStream(Path.Combine(_workDir, iconFile), FileMode.Create))
                {
                    icon.Save(s);
                }

                WriteLine("Plugin Icon extracted from Plugin Assembly.");
            }
        }
        catch (Exception ex)
        {
            WriteLine($"ICON EXTRACT ERROR: {ex.Message}", false);
            iconFile = null;
        }

        return iconFile;
    }

    private static string GetWorkingDirectory()
    {
        var path = Environment.ExpandEnvironmentVariables(@"%TEMP%\WrapperBuilder");
        var directory = new DirectoryInfo(path);
        if (!directory.Exists)
        {
            directory.Create();
        }

        return directory.FullName;
    }

    private static List<string> LoadExcludedList()
    {
        var list = new List<string>();
        var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
        domain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
        domain.SetData("pluginDll", _pluginAssemblyFile);
        domain.SetData("pluginType", _pluginType);
        WriteLine("Excluded methods:");
        WriteLine($"  Plugin Assembly '{_pluginAssemblyFile}'");
        domain.DoCallBack(LoadExcludedMethods);

        var pList = (List<string>)domain.GetData("methods");
        foreach (var method in pList)
        {
            WriteLine($"   - {method}");
        }

        list.AddRange(pList);
        if (_pluginType.Equals(PluginType.FileSystem))
        {
            pList.Clear();
            WriteLine(
                string.Format(
                    (string.IsNullOrEmpty(_contentAssemblyFile) ? "  No Content Assembly, try " : "  Content Assembly ") + "'{0}'",
                    _contentAssemblyFile ?? _pluginAssemblyFile));
            // System plugins can contain some methods of content plugin
            domain.SetData("pluginDll", _contentAssemblyFile ?? _pluginAssemblyFile);
            domain.SetData("pluginType", PluginType.Content);
            try
            {
                domain.DoCallBack(LoadExcludedMethods);
                pList = (List<string>)domain.GetData("methods");
                foreach (var method in pList)
                {
                    WriteLine($"   - {method}");
                }

                list.AddRange(pList);
            }
            catch (PluginNotImplementedException)
            {
                WriteLine("  -- Content interface is NOT implemented, exclude all Content methods:");
                pList.AddRange(_pluginMandatoryMethods[PluginType.Content]);
                pList.AddRange(_pluginOptionalMethods[PluginType.Content]);
                pList.AddRange(_pluginOtherMethods[PluginType.Content]);
                foreach (var method in pList)
                {
                    WriteLine($"   - {method}");
                }

                list.AddRange(pList);
            }
        }

        AppDomain.Unload(domain);
        return list;
    }

    private static void LoadExcludedMethods()
    {
        var assemblyPath = (string)AppDomain.CurrentDomain.GetData("pluginDll");
        _pluginType = (PluginType)AppDomain.CurrentDomain.GetData("pluginType");
        var methods = GetExcludedMethods(assemblyPath, _pluginType);
        AppDomain.CurrentDomain.SetData("methods", methods);
    }

    private static List<string> GetExcludedMethods(string assemblyPath, PluginType pType)
    {
        if (string.IsNullOrEmpty(assemblyPath)
            || !File.Exists(assemblyPath)
            || pType.Equals(PluginType.Unknown))
        {
            return new List<string>();
        }

        var assemblyOk = false;

        var exclMethods = new List<string>(_pluginOptionalMethods[pType]);
        var assembly = TcPluginLoader.AssemblyReflectionOnlyLoadFrom(assemblyPath);
        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.GetInterface(TcUtils.PluginInterfaces[pType]) != null)
            {
                var methodsMissed = string.Empty;
                var typeMethods = new List<string>();
                var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                foreach (var method in type.GetMethods(bindingFlags))
                {
                    typeMethods.Add(method.Name);
                }

                // Check if all mandatory methods are implemented in the type
                foreach (var method in _pluginMandatoryMethods[pType])
                {
                    if (!typeMethods.Contains(method))
                    {
                        methodsMissed += $"{method},";
                    }
                }

                if (methodsMissed.Length == 0)
                {
                    // all mandatory methods are implemented
                    assemblyOk = true;
                    foreach (var method in _pluginOptionalMethods[pType])
                    {
                        if (typeMethods.Contains(method))
                        {
                            exclMethods.Remove(method);
                        }
                        else
                        {
                            foreach (var substMethod in SubstituteMethods(method, pType))
                            {
                                if (typeMethods.Contains(substMethod))
                                {
                                    exclMethods.Remove(method);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (!assemblyOk)
        {
            throw new PluginNotImplementedException(string.Format(ErrorMsg7, TcUtils.PluginNames[pType], assemblyPath));
        }

        return exclMethods;
    }

    private static IEnumerable<string> SubstituteMethods(string method, PluginType pType)
    {
        if (pType.Equals(PluginType.FileSystem) && method.Equals("ExecuteFile"))
        {
            return new[] { "ExecuteOpen", "ExecuteProperties", "ExecuteCommand" };
        }

        return new string[0];
    }

    private static List<string> LoadExportedList(string assemblyPath)
    {
        var domain = AppDomain.CreateDomain("Exported");
        domain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
        domain.SetData("assemblyPath", assemblyPath);
        domain.DoCallBack(LoadExportedMethods);
        var list = (List<string>)domain.GetData("methods");
        _wrapperAssemblyVersion = (string)domain.GetData("assemblyVersion");
        _pluginType = (PluginType)domain.GetData("pluginType");
        AppDomain.Unload(domain);
        return list;
    }

    private static void LoadExportedMethods()
    {
        var plugType = PluginType.Unknown;
        var assemblyFile = (string)AppDomain.CurrentDomain.GetData("assemblyPath");
        var assembly = TcPluginLoader.AssemblyReflectionOnlyLoadFrom(assemblyFile);
        var assemblyVersion = assembly.GetName().Version.ToString();
        foreach (var type in assembly.GetTypes())
        {
            var pi = type.GetProperty(
                PluginInterfacePropertyName,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (pi != null)
            {
                var pType = GetPluginType(pi.PropertyType);
                if (!pType.Equals(PluginType.Unknown))
                {
                    if (plugType.Equals(PluginType.Unknown))
                    {
                        plugType = pType;
                    }
                    else
                    {
                        throw new Exception(string.Format(ErrorMsg4, assemblyFile, _pluginExtensions[pType], _pluginExtensions[plugType]));
                    }
                }
            }
        }

        if (plugType.Equals(PluginType.Unknown))
        {
            throw new Exception(string.Format(ErrorMsg5, assemblyFile));
        }

        var methods = GetExportedMethods(assembly);
        AppDomain.CurrentDomain.SetData("methods", methods);
        AppDomain.CurrentDomain.SetData("assemblyVersion", assemblyVersion);
        AppDomain.CurrentDomain.SetData("pluginType", plugType);
    }

    private static PluginType GetPluginType(Type type)
    {
        foreach (var pType in TcUtils.PluginInterfaces.Keys)
        {
            if (type.GetInterface(TcUtils.PluginInterfaces[pType]) != null)
            {
                return pType;
            }
        }

        return PluginType.Unknown;
    }

    private static void ParseArgs(string[] args)
    {
        //TODO : parameter to create simple config file
        var showHelp = args.Length == 0;
        foreach (var arg in args)
        {
            if (arg.Equals("/?"))
            {
                showHelp = true;
                break;
            }

            switch (arg)
            {
                case "/wcx" when string.IsNullOrEmpty(_wrapperAssemblyFile):
                    _wrapperAssemblyFile = Path.Combine(_appFolder, "WcxWrapper.dll");
                    break;
                case "/wcx":
                    throw new Exception(ErrorMsg0);
                case "/wdx" when string.IsNullOrEmpty(_wrapperAssemblyFile):
                    _wrapperAssemblyFile = Path.Combine(_appFolder, "WdxWrapper.dll");
                    break;
                case "/wdx":
                    throw new Exception(ErrorMsg0);
                case "/wfx" when string.IsNullOrEmpty(_wrapperAssemblyFile):
                    _wrapperAssemblyFile = Path.Combine(_appFolder, "WfxWrapper.dll");
                    break;
                case "/wfx":
                    throw new Exception(ErrorMsg0);
                case "/wlx" when string.IsNullOrEmpty(_wrapperAssemblyFile):
                    _wrapperAssemblyFile = Path.Combine(_appFolder, "WlxWrapper.dll");
                    break;
                case "/wlx":
                    throw new Exception(ErrorMsg0);
                case "/qs" when string.IsNullOrEmpty(_wrapperAssemblyFile):
                    _wrapperAssemblyFile = Path.Combine(_appFolder, "QSWrapper.dll");
                    break;
                case "/qs":
                    throw new Exception(ErrorMsg0);
                default:
                    {
                        if (arg.StartsWith("/w="))
                        {
                            if (string.IsNullOrEmpty(_wrapperAssemblyFile))
                            {
                                _wrapperAssemblyFile = arg.Substring(3);
                            }
                            else
                            {
                                throw new Exception(ErrorMsg0);
                            }
                        }
                        else if (arg.StartsWith("/p="))
                        {
                            _pluginAssemblyFile = arg.Substring(3);
                        }
                        else if (arg.StartsWith("/c="))
                        {
                            _contentAssemblyFile = arg.Substring(3);
                        }
                        else if (arg.StartsWith("/o="))
                        {
                            _outputWrapperName = arg.Substring(3);
                        }
                        else if (arg.StartsWith("/a="))
                        {
                            _assemblerPath = arg.Substring(3);
                        }
                        else if (arg.StartsWith("/d="))
                        {
                            _disassemblerPath = arg.Substring(3);
                        }
                        else if (arg.StartsWith("/r="))
                        {
                            _rcPath = arg.Substring(3);
                        }
                        else if (arg.StartsWith("/i="))
                        {
                            _iconFileName = arg.Substring(3);
                        }
                        else if (arg.Equals("/ipa"))
                        {
                            _iconFromPluginAssembly = true;
                        }
                        else if (arg.Equals("/v"))
                        {
                            _verbose = true;
                        }
                        else if (arg.Equals("/release"))
                        {
                            _ilasmArgs.Add("/optimize");
                        }
                        else if (arg.Equals("/x32"))
                        {
                            _x64Flag = false;
                        }
                        else if (arg.Equals("/x64"))
                        {
                            _x32Flag = false;
                        }
                        else if (arg.Equals("/pause"))
                        {
                            _pause = true;
                        }

                        break;
                    }
            }
        }

        if (showHelp)
        {
            foreach (var str in _usageInfo)
                Console.WriteLine(str);

            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(_wrapperAssemblyFile) || !File.Exists(_wrapperAssemblyFile))
            throw new Exception(string.Format(ErrorMsg1, _wrapperAssemblyFile));

        if (string.IsNullOrEmpty(_pluginAssemblyFile) || !File.Exists(_pluginAssemblyFile))
            throw new Exception(string.Format(ErrorMsg11, _pluginAssemblyFile));

        if (string.IsNullOrEmpty(_assemblerPath) && _appSettings != null)
            _assemblerPath = _appSettings["assemblerPath"];

        if (string.IsNullOrEmpty(_disassemblerPath) && _appSettings != null)
            _disassemblerPath = _appSettings["disassemblerPath"];

        if (string.IsNullOrEmpty(_rcPath) && _appSettings != null)
            _rcPath = _appSettings["rcPath"];

        if (string.IsNullOrEmpty(_assemblerPath) || !File.Exists(_assemblerPath))
            throw new Exception(ErrorMsg2);

        if (string.IsNullOrEmpty(_disassemblerPath) || !File.Exists(_disassemblerPath))
            throw new Exception(ErrorMsg3);

        if (!_x32Flag && !_x64Flag)
            throw new Exception(ErrorMsg10);

        WriteLine($"IL Disassembler: '{_disassemblerPath}'");
        WriteLine($"IL Assembler   : '{_assemblerPath}'");
    }

    private static void SetOutputNames()
    {
        if (string.IsNullOrEmpty(_outputWrapperName))
        {
            _outputWrapperFolder = Path.GetDirectoryName(_pluginAssemblyFile);
            _outputWrapperName = _pluginType == PluginType.QuickSearch ? "tcmatch" : Path.GetFileNameWithoutExtension(_pluginAssemblyFile);
        }
        else
        {
            _outputWrapperFolder = Path.GetDirectoryName(_outputWrapperName);
            if (!Path.IsPathRooted(_outputWrapperFolder))
                _outputWrapperFolder = Path.Combine(Path.GetDirectoryName(_pluginAssemblyFile), _outputWrapperFolder);

            _outputWrapperName = Path.GetFileNameWithoutExtension(_outputWrapperName);
        }

        if (!Directory.Exists(_outputWrapperFolder))
            Directory.CreateDirectory(_outputWrapperFolder);

        _outputWrapperName = Path.Combine(_outputWrapperFolder, _outputWrapperName);
    }

    private const string DllExportAttributeStr =
        ".custom instance void [TcPluginInterface]OY.TotalCommander.TcPluginInterface.DllExportAttribute";

    private static void ProcessSource(string sourcePath, string outPath, List<string> exportedMethods, List<string> excludedMethods)
    {
        var prefix = _pluginMethodPrefixes[_pluginType] ?? string.Empty;
        var cntPrefix = (_pluginType.Equals(PluginType.FileSystem) ? _pluginMethodPrefixes[PluginType.Content] : null)
                        ?? string.Empty;
        foreach (var method in excludedMethods)
        {
            var exclMethod = prefix + method;
            var cntExclMethod = string.IsNullOrEmpty(cntPrefix) ? null : prefix + cntPrefix + method;
            if (exportedMethods.Contains(exclMethod))
            {
                exportedMethods.Remove(exclMethod);
                // Check if Unicode method exists
                if (exportedMethods.Contains($"{exclMethod}W"))
                {
                    exportedMethods.Remove($"{exclMethod}W");
                }
            }
            else if (!string.IsNullOrEmpty(cntExclMethod) && exportedMethods.Contains(cntExclMethod))
            {
                exportedMethods.Remove(cntExclMethod);
                // Check if Unicode method exists
                if (exportedMethods.Contains($"{cntExclMethod}W"))
                {
                    exportedMethods.Remove($"{cntExclMethod}W");
                }
            }
            else
            {
                WriteLine($"  Excluded method '{method}' is not in exported list.");
            }
        }

        using var output = new StreamWriter(outPath, false, Encoding.Default);

        var methodIndex = 0;
        var openBraces = 0;
        var isMethodStatic = false;
        var isMethodExcluded = false;
        var methodName = "<NONE>";
        var methodHeaders = new List<string>();

        foreach (var srcLine in File.ReadAllLines(sourcePath, Encoding.Default))
        {
            var line = srcLine.TrimStart(' ');
            if (line.StartsWith(".method"))
            {
                isMethodStatic = line.Contains(" static ");
                methodName = "<UNKNOWN>";
                methodHeaders.Clear();
            }

            if (methodName.Equals("<UNKNOWN>"))
            {
                var pos = srcLine.IndexOf('(');
                if (pos > 0)
                {
                    var pos1 = srcLine.LastIndexOf(' ', pos);
                    if (pos1 < 0)
                    {
                        pos1 = 0;
                    }

                    var mName = srcLine.Substring(pos1 + 1, pos - pos1 - 1).Trim();
                    if (!mName.Equals("marshal"))
                    {
                        methodName = mName;
                    }
                }

                if (methodName.Equals("<UNKNOWN>"))
                {
                    methodHeaders.Add(srcLine);
                }
                else
                {
                    isMethodExcluded = excludedMethods.Contains(methodName);
                    if (!isMethodExcluded && methodName.EndsWith("W"))
                    {
                        isMethodExcluded =
                            excludedMethods.Contains(methodName.Substring(0, methodName.Length - 1));
                    }

                    if (!isMethodExcluded && methodHeaders.Count > 0)
                    {
                        foreach (var s in methodHeaders)
                        {
                            output.WriteLine(s);
                        }

                        methodHeaders.Clear();
                    }
                }
            }

            if (!isMethodExcluded && line.StartsWith(DllExportAttributeStr))
            {
                foreach (var ch in line)
                {
                    switch (ch)
                    {
                        case '(':
                            openBraces++;
                            break;
                        case ')':
                            openBraces--;
                            break;
                    }
                }

                if (isMethodStatic)
                {
                    output.WriteLine(".export [{0}] as {1}", methodIndex + 1, exportedMethods[methodIndex]);
                    methodIndex++;
                }

                continue;
            }

            if (!isMethodExcluded && openBraces > 0)
            {
                foreach (var ch in line)
                {
                    switch (ch)
                    {
                        case '(':
                            openBraces++;
                            break;
                        case ')':
                            openBraces--;
                            break;
                    }
                }

                continue;
            }

            if (isMethodExcluded && line.StartsWith(@"} // end of method"))
            {
                isMethodExcluded = false;
                methodName = "<NONE>";
                continue;
            }

            if (!isMethodExcluded && !methodName.Equals("<UNKNOWN>"))
                output.WriteLine(srcLine);
        }
    }

    private static Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
    {
        try
        {
            return Assembly.ReflectionOnlyLoad(args.Name);
        }
        catch (FileNotFoundException)
        {
            var mainAssemblyPath = (string)AppDomain.CurrentDomain.GetData("pluginDll");
            var assemblyName = args.Name;
            if (assemblyName.IndexOf(',') > 0)
            {
                assemblyName = assemblyName.Substring(0, assemblyName.IndexOf(','));
            }

            if (string.IsNullOrEmpty(Path.GetExtension(assemblyName)))
            {
                assemblyName += ".dll";
            }

            return Assembly.ReflectionOnlyLoadFrom(Path.Combine(Path.GetDirectoryName(mainAssemblyPath), assemblyName));
        }
    }

    private static void WriteLine(string text) => WriteLine(text, true);

    private static void WriteLine(string text, bool detailed)
    {
        if (_verbose || !detailed)
            Console.WriteLine(text);
    }

    #endregion Private Methods
}
