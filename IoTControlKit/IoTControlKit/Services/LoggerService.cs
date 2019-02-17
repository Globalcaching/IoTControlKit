using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public class LoggerService : BaseService
    {
        //matching view!
        public class LogLine
        {
            public long Id { get; set; }
            public string date { get; set; }
            public string level { get; set; }
            public string text { get; set; }
        }

        private static LoggerService _uniqueInstance = null;
        private static object _lockObject = new object();

        private List<LogLine> _logBuffer = new List<LogLine>();
        private long _uniqueLogId = 0;

        private LoggerService()
        {
        }

        public static LoggerService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new LoggerService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public void Download(Stream response)
        {
            using (var zipArchive = new ZipArchive(new WrappedStream(response), ZipArchiveMode.Create))
            {
                AddLogsToArchive(zipArchive);
            }
        }

        public void AddLogsToArchive(ZipArchive zipArchive)
        {
            try
            {
                NLog.LogManager.Flush();
            }
            catch
            {
            }

            AddLogFolderToArchive(zipArchive, Path.Combine(Helpers.EnvironmentHelper.RootDataFolder, "Logs"), "Logs/IoTControlKit");
        }

        private void AddLogFolderToArchive(ZipArchive zipArchive, string folder, string archiveFolder)
        {
            if (Directory.Exists(folder))
            {
                var files = new List<string>();
                files.AddRange(Directory.GetFiles(folder, "*.log"));
                foreach (var f in files)
                {
                    var zipEntry = zipArchive.CreateEntry($"{archiveFolder}/{Path.GetFileName(f)}");
                    using (var zipStream = zipEntry.Open())
                    {
                        using (var s = File.Open(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var r = new StreamReader(s))
                            using (var w = new StreamWriter(zipStream))
                            {
                                w.Write(r.ReadToEnd());
                            }
                        }
                    }
                    //zipArchive.CreateEntryFromFile(f, Path.GetFileName($"CortexxCore/{f}")); //in use
                }
            }
        }

        public LogLine[] LastLogs
        {
            get
            {
                LogLine[] result = null;
                lock (_logBuffer)
                {
                    result = _logBuffer.ToArray();
                }
                return result;
            }
        }

        public LogLine[] GetLastLogs(long lastLogId)
        {
            LogLine[] result = null;
            lock (_logBuffer)
            {
                var index = _logBuffer.FindIndex(x => x.Id == lastLogId);
                if (index < 0)
                {
                    result = _logBuffer.ToArray();
                }
                else if (index == _logBuffer.Count() - 1)
                {
                    result = new LogLine[0];
                }
                else
                {
                    result = _logBuffer.Skip(index).ToArray();
                }
            }
            return result;
        }

        private void AddLog(LogLevel level, Exception ex, string message, params object[] args)
        {
            var l = new LogLine();
            l.date = DateTime.Now.ToString("HH:mm:ss.fff");
            switch (level)
            {
                case LogLevel.Debug:
                    l.level = "1";
                    break;
                case LogLevel.Trace:
                    l.level = "12";
                    break;
                case LogLevel.Information:
                    l.level = "123";
                    break;
                case LogLevel.Warning:
                    l.level = "1234";
                    break;
                case LogLevel.Error:
                    l.level = "12345";
                    break;
                case LogLevel.Critical:
                    l.level = "123456";
                    break;
            }
            if (args != null && args.Length > 0)
            {
                l.text = string.Format(message, args);
            }
            else
            {
                l.text = message;
            }
            if (ex != null)
            {
                l.text = $"{l.text}\r\nException:{ex.Message}";
            }
            lock (_logBuffer)
            {
                _uniqueLogId++;
                if (_uniqueLogId == long.MaxValue)
                {
                    _uniqueLogId = 0;
                    foreach (var el in _logBuffer)
                    {
                        _uniqueLogId++;
                        el.Id = _uniqueLogId;
                    }
                    _uniqueLogId++;
                }
                l.Id = _uniqueLogId;

                _logBuffer.Add(l);
                while (_logBuffer.Count > 10000)
                {
                    _logBuffer.RemoveAt(0);
                }
            }
        }

        public void SetLogLevel(string level)
        {
            SettingsService.Instance.LogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), level);
            NotificationService.Instance.AddSuccessMessage($"Log level succesfully changed to {level}");
        }


        public void LogCritical(Exception exception, string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Critical)
            {
                Program.Logger?.Fatal(exception, message, args);
            }
            Serilog.Log.Fatal(exception, message, args);
            AddLog(LogLevel.Critical, exception, message, args);
        }

        public void LogCritical(string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Critical)
            {
                Program.Logger?.Fatal(message, args);
            }
            AddLog(LogLevel.Critical, null, message, args);
        }

        public void LogDebug(Exception exception, string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Debug)
            {
                Program.Logger?.Debug(exception, message, args);
            }
            Serilog.Log.Fatal(message, args);
            AddLog(LogLevel.Debug, exception, message, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Debug)
            {
                Program.Logger?.Debug(message, args);
            }
            AddLog(LogLevel.Debug, null, message, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Error)
            {
                Program.Logger?.Error(exception, message, args);
            }
            AddLog(LogLevel.Error, exception, message, args);
        }

        public void LogError(string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Error)
            {
                Program.Logger?.Error(message, args);
            }
            AddLog(LogLevel.Error, null, message, args);
        }

        public void LogInformation(Exception exception, string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Information)
            {
                Program.Logger?.Info(exception, message, args);
            }
            AddLog(LogLevel.Information, exception, message, args);
        }

        public void LogInformation(string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Information)
            {
                Program.Logger?.Info(message, args);
            }
            AddLog(LogLevel.Information, null, message, args);
        }

        public void LogTrace(Exception exception, string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Trace)
            {
                Program.Logger?.Trace(exception, message, args);
            }
            AddLog(LogLevel.Trace, exception, message, args);
        }

        public void LogTrace(string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Trace)
            {
                Program.Logger?.Trace(message, args);
            }
            AddLog(LogLevel.Trace, null, message, args);
        }

        public void LogWarning(Exception exception, string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Warning)
            {
                Program.Logger?.Warn(exception, message, args);
            }
            AddLog(LogLevel.Warning, exception, message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            if (SettingsService.Instance.LogLevel <= LogLevel.Warning)
            {
                Program.Logger?.Warn(message, args);
            }
            AddLog(LogLevel.Warning, null, message, args);
        }
    }

}
