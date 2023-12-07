using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace ArgusIPMI
{
    public class Logger
    {
        private static readonly Lazy<Logger> instance = new(() => new Logger());
        private readonly string logFilePath;

        private Logger()
        {
            var logFolderName = "log";
            var logFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFolderName);
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            var logFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            logFilePath = Path.Combine(logFolderPath, logFileName);
            File.Create(logFilePath).Close();
        }

        public static Logger Instance => instance.Value;

        public void Log(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to log file: " + ex.Message);
            }
        }
    }
}