using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace ArgusIPMI
{
    public class Logger
    {
        private static readonly Lazy<Logger> instance = new Lazy<Logger>(() => new Logger());
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

    [Serializable]
    public class Settings
    {
        public string IpAddress { get; set; } = "0.0.0.0";
        public string Username { get; set; } = "change-me";
        public string Password { get; set; } = "change-me";
    }

    public class ConfigManager
    {
        private readonly string configFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        private readonly string configFilePath;

        public ConfigManager()
        {
            configFilePath = Path.Combine(configFolderPath, "config.xml");
            EnsureConfigFile();
        }

        private void EnsureConfigFile()
        {
            if (!Directory.Exists(configFolderPath)) Directory.CreateDirectory(configFolderPath);
            if (!File.Exists(configFilePath)) SaveSettings(new Settings());
        }

        public Settings LoadSettings()
        {
            try
            {
                using var stream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read);
                var serializer = new XmlSerializer(typeof(Settings));
                return (serializer.Deserialize(stream) as Settings) ?? new Settings();
            }
            catch
            {
                return new Settings();
            }
        }

        public void SaveSettings(Settings settings)
        {
            try
            {
                using
                var stream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write);
                var serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(stream, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving settings: " + ex.Message);
            }
        }
    }

    public class IPMIToolWrapper(string ipmitoolPath)
    {
        private readonly string toolPath = ipmitoolPath;

        public async Task<string> GetSensorListAsync(string ipAddress, string username, string password)
        {
            string command = "sensor list";
            return await ExecuteCommandAsync(ipAddress, username, password, command);
        }

        public async Task<string> ExecuteCommandAsync(string ipAddress, string username, string password, string command)
        {
            string result = string.Empty;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = $"-I lanplus -H {ipAddress} -U {username} -P {password} {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                using var reader = process.StandardOutput;
                result = await reader.ReadToEndAsync(); // Wait for the entire output
                process.WaitForExit(); // Ensure the process has completed
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"Error executing command: {command} - Exception: {ex.Message}");
                Console.WriteLine("Error.");
            }
            return result;
        }
        public static void SaveSensorData(string data)
        {
            var dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataFolderPath)) Directory.CreateDirectory(dataFolderPath);

            var filePath = Path.Combine(dataFolderPath, "data.txt"); // Fixed file name
            File.WriteAllText(filePath, data); // Overwrite the file with new data
            Logger.Instance.Log("Data wrote to file.");
        }
    }

    public class Program
    {
        static async Task Main(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            Console.Title = "Argus IPMI Host";

            var configManager = new ConfigManager();
            var settings = configManager.LoadSettings() ?? new Settings();

            Logger.Instance.Log("Init.");
            Console.WriteLine("Initialized.");

            var projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
            if (projectDirectory == null)
            {
                return;
            }

            var ipmiToolPath = Path.Combine(projectDirectory, "ipmitool", "ipmitool.exe");
            if (!File.Exists(ipmiToolPath))
            {
                return;
            }

            var ipmiWrapper = new IPMIToolWrapper(ipmiToolPath);

            // Start the web server in a separate task
            var webServerTask = WebServerHost.StartWebServer();
            Console.WriteLine("Starting the Webserver.");

            // Start the sensor data retrieval loop in another task
            var sensorDataTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (string.IsNullOrEmpty(settings.IpAddress) || string.IsNullOrEmpty(settings.Username) || string.IsNullOrEmpty(settings.Password))
                    {
                        break; // Exit the loop if settings are invalid
                    }

                    var sensorData = await ipmiWrapper.GetSensorListAsync(settings.IpAddress, settings.Username, settings.Password);
                    IPMIToolWrapper.SaveSensorData(sensorData);
                }
            });

            // Wait for both tasks to complete (in practice, the sensor data task is infinite)
            await Task.WhenAll(webServerTask, sensorDataTask);
        }
    }
}