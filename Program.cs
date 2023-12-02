using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ArgusIPMI
{
    public class Program
    {
        static IPMIToolWrapper ipmiWrapper;
        static FileSystemWatcher configWatcher;
        static ConfigManager configManager;
        static Settings currentSettings;

        static async Task Main(string[] args)
        {
            Logger.Instance.Log("Init.");
            Console.WriteLine("Init.");
            ArgumentNullException.ThrowIfNull(args);
            Console.Title = "Argus IPMI Host";

            configManager = new ConfigManager();
            currentSettings = configManager.LoadSettings();

            var projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
            if (projectDirectory == null)
            {
                Logger.Instance.Log("Project directory not found.");
                Console.WriteLine("Project directory not found.");
                return;
            }

            var ipmiToolPath = Path.Combine(projectDirectory, "ipmitool", "ipmitool.exe");
            if (!File.Exists(ipmiToolPath))
            {
                Logger.Instance.Log("IPMI tool not found.");
                Console.WriteLine("IPMI tool not found.");
                return;
            }

            ipmiWrapper = new IPMIToolWrapper(ipmiToolPath);

            var connectionSuccessful = await TryIPMIConnection();
            if (!connectionSuccessful)
            {
                Logger.Instance.Log("Initial IPMI connection attempt failed. Please check your settings.");
                Console.WriteLine("Initial IPMI connection attempt failed. Please check your settings.");
            }

            SetupConfigFileWatcher();

            var webServerTask = WebServerHost.StartWebServer(ipmiWrapper);
            Logger.Instance.Log("Starting the Webserver.");
            Console.WriteLine("Starting the Webserver.");

            if (connectionSuccessful)
            {
                var sensorDataTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        if (string.IsNullOrEmpty(currentSettings.IpAddress) || string.IsNullOrEmpty(currentSettings.Username) || string.IsNullOrEmpty(currentSettings.Password))
                        {
                            break; // Exit the loop if settings are invalid
                        }

                        var sensorData = await ipmiWrapper.GetSensorListAsync(currentSettings.IpAddress, currentSettings.Username, currentSettings.Password);
                        IPMIToolWrapper.SaveSensorData(sensorData);
                    }
                });

                await Task.WhenAll(webServerTask, sensorDataTask);
            }
            else
            {
                await webServerTask;
            }
        }

        static void SetupConfigFileWatcher()
        {
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.xml");
            configWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(configFilePath),
                Filter = Path.GetFileName(configFilePath),
                NotifyFilter = NotifyFilters.LastWrite
            };

            configWatcher.Changed += OnConfigFileChanged;
            configWatcher.EnableRaisingEvents = true;
        }

        static async void OnConfigFileChanged(object source, FileSystemEventArgs e)
        {
            await Task.Delay(500);

            Logger.Instance.Log("Config file changed. Reloading settings.");
            currentSettings = configManager.LoadSettings();

            var connectionSuccessful = await TryIPMIConnection();
            if (!connectionSuccessful)
            {
                Logger.Instance.Log("IPMI connection attempt with new settings failed.");
            }
        }
        public static async Task<bool> TryIPMIConnection()
        {
            var configManager = new ConfigManager();
            var settings = configManager.LoadSettings();

            try
            {
                var sensorData = await ipmiWrapper.GetSensorListAsync(currentSettings.IpAddress, currentSettings.Username, currentSettings.Password);

                if (sensorData.Contains("Error:"))
                {
                    Logger.Instance.Log("IPMI Connection Error: Unable to establish session.");
                    Console.WriteLine("IPMI Connection Error: Unable to establish session.");
                    IPMIToolWrapper.ClearSensorData(sensorData);
                    return false;
                }
                else if (sensorData.Contains("Error:"))
                {
                    Logger.Instance.Log("IPMI Connection Error: Incorrect password.");
                    Console.WriteLine("IPMI Connection Error: Incorrect password.");
                    IPMIToolWrapper.ClearSensorData(sensorData);
                    return false;
                }
                // You can add more specific error checks as needed

                // If no errors, return true
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"IPMI Connection Exception: {ex.Message}");
                Console.WriteLine($"IPMI Connection Exception: {ex.Message}");
                return false;
            }
        }
    }
}