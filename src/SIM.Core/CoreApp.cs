﻿namespace SIM.Core
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using log4net.Config;
  using log4net.Core;
  using log4net.Layout;
  using log4net.Util;
  using JetBrains.Annotations;
  using Sitecore.Diagnostics.Logging;
  using SIM.Core.Logging;
  using SIM.Extensions;
  using SIM.FileSystem;

  public static class CoreApp
  {
    private const string FirstRunFileName = "first-run.txt";
    private const string LastRunFileName = "last-run.txt";

    public static bool IsVeryFirstRun
    {
      get
      {
        if (File.Exists(LastRunFileName))
        {
          // when last-run exists it cannot be very first run
          return false;
        }

        return File.Exists(FirstRunFileName);
      }
    }

    public static bool HasBeenUpdated
    {
      get
      {
        return !ApplicationManager.AppVersion.Equals(PreviousVersion, StringComparison.OrdinalIgnoreCase);
      }
    }

    public static string PreviousVersion
    {
      get
      {
        if (!File.Exists(LastRunFileName))
        {
          return string.Empty;
        }

        return File.ReadAllText(LastRunFileName).Trim(" \r\n".ToCharArray());
      }
    }

    public static void WriteLastRunVersion()
    {
      File.WriteAllText(LastRunFileName, ApplicationManager.AppVersion);
    }

    public static void Exit()
    {
      Log.Info(string.Format("Shutting down"));
    }

    public static void LogMainInfo()
    {
      try
      {
        var nativeArgs = Environment.GetCommandLineArgs();
        var commandLineArgs = nativeArgs.Skip(1).ToArray();
        var argsToLog = commandLineArgs.Length > 0 ? string.Join("|", commandLineArgs) : "<NO ARGUMENTS>";

        Log.Info(string.Format("**********************************************************************"));
        Log.Info(string.Format("**********************************************************************"));
        Log.Info(string.Format("Sitecore Instance Manager started"));
        Log.Info($"Version: {ApplicationManager.AppVersion}");
        Log.Info($"Revision: {ApplicationManager.AppRevision}");
        Log.Info($"Label: {ApplicationManager.AppLabel}");
        Log.Info($"IsQA: {ApplicationManager.IsQA}");
        Log.Info($"Executable: {nativeArgs.FirstOrDefault() ?? ApplicationManager.ProcessName}");
        Log.Info($"Arguments: {argsToLog}");
        Log.Info($"Directory: {Environment.CurrentDirectory}");
        Log.Info(string.Format("**********************************************************************"));
        Log.Info(string.Format("**********************************************************************"));
      }
      catch
      {
        Debug.WriteLine("Error during log main info");
      }
    }

    public static void InitializeLogging()
    {
      Log.Initialize(new Log4NetLogProvider());

      var logConfig = new FileInfo("Log.config");
      if (logConfig.Exists)
      {
        XmlConfigurator.Configure(logConfig);
      }
      else
      {
        var infoLogger = new LogFileAppender
        {
          AppendToFile = true,
          File = "hard-coded",
          Layout = new PatternLayout("%4t %d{ABSOLUTE} %-5p %m%n"),
          SecurityContext = new WindowsSecurityContext(),
          Threshold = Level.Info
        };

        var debugLogger = new LogFileAppender
        {
          AppendToFile = true,
          File = "$(debugPath)",
          Layout = new PatternLayout("%4t %d{ABSOLUTE} %-5p %m%n"),
          SecurityContext = new WindowsSecurityContext(),
          Threshold = Level.Debug
        };

        BasicConfigurator.Configure(infoLogger, debugLogger);
      }
    }

    public static void DeleteTempFolders()
    {
      try
      {
        FileSystem.Local.Directory.DeleteTempFolders();
      }
      catch (Exception ex)
      {
        Log.Error(ex, string.Format("Deleting temp folders caused an exception"));
      }
    }

    public static void OpenFile(string path)
    {
      RunApp("explorer.exe", path.Replace('/', '\\'));
    }

    public static void OpenFolder(string path)
    {
      OpenFile(path);
    }

    public static void OpenInBrowser(string url, bool isFrontEnd, string browser = null, [CanBeNull] string[] parameters = null)
    {
      var app = browser.EmptyToNull() ?? (isFrontEnd ? CoreAppSettings.AppBrowsersFrontend.Value : CoreAppSettings.AppBrowsersBackend.Value);
      if (!string.IsNullOrEmpty(app))
      {
        var arguments = parameters != null ? parameters.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() : new List<string>();
        arguments.Add(url);
        RunApp(app, arguments.ToArray());

        return;
      }

      OpenFile(url);
    }

    public static Process RunApp(string app, params string[] @params)
    {
      using (new ProfileSection("Running app"))
      {
        ProfileSection.Argument("app", app);
        ProfileSection.Argument("@params", @params);

        var resultParams = string.Join(" ", @params.Select(x => x.Trim('\"')).Select(x => x.Contains(" ") || x.Contains("=") ? "\"" + x + "\"" : x));
        Log.Debug($"resultParams: {resultParams}");

        var process = Process.Start(app, resultParams);

        return ProfileSection.Result(process);
      }
    }

    public static Process RunApp(ProcessStartInfo startInfo)
    {
      return Process.Start(startInfo);
    }
  }
}