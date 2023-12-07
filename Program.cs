using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ArgusIPMI
{
    public class Program
    {
        static IPMIToolWrapper? ipmiWrapper; 
        static ConfigManager? configManager; 
        static Settings? currentSettings; 

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

            var ipmiToolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ipmitool", "ipmitool.exe");

            if (!File.Exists(ipmiToolPath))
            {
                Logger.Instance.Log("IPMI tool not found.");
                Console.WriteLine("IPMI tool not found.");
                return;
            }

            ipmiWrapper = new IPMIToolWrapper(ipmiToolPath);

            var executor = new Executor(ipmiWrapper, currentSettings);

            var connectionSuccessful = await IPMIChecker.TryIPMIConnection(ipmiWrapper, currentSettings);
            if (!connectionSuccessful)
            {
                Logger.Instance.Log("Initial IPMI connection attempt failed. Please check your settings.");
                Console.WriteLine("Initial IPMI connection attempt failed. Please check your settings.");
            }

            if (currentSettings == null)
            {
                Logger.Instance.Log("Settings not loaded.");
                Console.WriteLine("Settings not loaded.");
                return;
            }

            var webServerTask = WebServerHost.StartWebServer(executor);
            Logger.Instance.Log("Starting the Webserver.");
            Console.WriteLine("Starting the Webserver.");

            if (connectionSuccessful)
            {
                var sensorDataTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        if (string.IsNullOrEmpty(currentSettings?.IpAddress) || string.IsNullOrEmpty(currentSettings?.Username) || string.IsNullOrEmpty(currentSettings?.Password))
                        {
                            break;
                        }
                        if (ipmiWrapper != null)
                        {
                            var sensorData = await ipmiWrapper.GetSensorListAsync(currentSettings.IpAddress, currentSettings.Username, currentSettings.Password);
                            IPMIToolWrapper.SaveSensorData(sensorData);
                        }
                    }
                });

                await Task.WhenAll(webServerTask, sensorDataTask);
            }
            else
            {
                await webServerTask;
            }
        }
    }
}