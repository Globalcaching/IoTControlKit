using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Helpers
{
    public class FileHelper
    {
        public static string EnsureValidFileName(string fn)
        {
            var result = fn.ToArray();
            var validChars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890._- ".ToArray();
            for (var i = 0; i < fn.Length; i++)
            {
                if (!validChars.Contains(result[i]))
                {
                    result[i] = '_';
                }
            }
            return string.Join("", result);
        }

        public static NLog.Targets.FileTarget CreateNLogFileTarget(string fileName, NLog.Layouts.Layout layout = null)
        {
            var result = new NLog.Targets.FileTarget()
            {
                Name = fileName,
                FileName = fileName,
                Layout = layout ?? "${longdate}|${uppercase:${level}}|${message} ${exception:format=tostring}",
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Rolling,
                MaxArchiveFiles = 10,
                ArchiveAboveSize = 5 * 1000 * 1000
            };
            return result;
        }

        public static NLog.Logger CreateNLogLogger(string fileName, NLog.Layouts.Layout layout = null)
        {
            var config = new NLog.Config.LoggingConfiguration();
            var target = CreateNLogFileTarget(fileName, layout);
            config.AddTarget(target);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, target);
            var factory = new NLog.LogFactory(config);

            var result = factory.GetCurrentClassLogger();
            return result;
        }
    }
}
