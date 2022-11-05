using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole
{
    public static class FileLogger
    {
        static string _logFileName = "Logging";
        public static void SetLogFileName(string logFileName)
        {
            _logFileName = logFileName;
        }

        public static void Log(string source, string message, bool append = true)
        {
            string text = $"Source: {source} - {message}";

            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string location = Path.Combine(executableLocation, $"{_logFileName}.txt");

            using StreamWriter file = new(location, append: true);
            file.WriteLine(text);
        }
    }
}
