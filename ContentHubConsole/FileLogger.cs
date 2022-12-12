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
            try
            {
                string text = $"Source: {source} - {message}";

                string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string location = Path.Combine(executableLocation, $"{_logFileName}.txt");

                using StreamWriter file = new(location, append);
                file.WriteLine(text);
            }
            catch (Exception ex)
            {
            }
        }

        public static void AddToFailedUploadLog(string filePath)
        {
            try
            {
                var existingPaths = new List<string>();

                StreamReader reader = null;
                if (File.Exists(Program.FileLoggerLocation))
                {
                    reader = new StreamReader(File.OpenRead(Program.FileLoggerLocation));
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        existingPaths.Add(line);
                    }

                    reader.Close();
                    reader.Dispose();
                }
                else
                {
                    Console.WriteLine("File doesn't exist");
                }

                if (!existingPaths.Any(a => a.Equals(filePath)))
                {
                    using StreamWriter file = new(Program.FileLoggerLocation, append: true);
                    file.WriteLine(filePath);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
