using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;


// I want to add a few options in my config.xml, like: 
// - Logging option, that would enable/disable logging entirely (enabled by default)
// - WebServer option - that would enable/disable the WebServer completely  (enabled by default)
// - WebServer port and IP (currently is hardcoded to "http://*:5000"
// - First response delay (currently 10s)
// - Executor time (currently 1000ms)
// 
// 
// So, let's start with the ConfigManager.xml first. Then we will touch the other modules. 
// here's it is: 

namespace ArgusIPMI
{
    [Serializable]
    public class Settings
    {
        public string IpAddress { get; set; } = "0.0.0.0";
        public string Username { get; set; } = "root";
        public string Password { get; set; } = "password";
        public bool EnableLogging { get; set; } = true; // Enabled by default
        public bool EnableWebServer { get; set; } = true; // Enabled by default
        public string WebServerHostname { get; set; } = "http://*"; // Default hostname
        public int WebServerPort { get; set; } = 5000; // Default port
        public int FirstResponseDelay { get; set; } = 10000; // Default 10 seconds
        public int ExecutorTimeInterval { get; set; } = 1000; // Default 1000 milliseconds
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
            if (!Directory.Exists(configFolderPath))
                Directory.CreateDirectory(configFolderPath);
            if (!File.Exists(configFilePath))
                SaveSettings(new Settings());
        }

        public Settings LoadSettings()
        {
            try
            {
                using var stream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read);
                var serializer = new XmlSerializer(typeof(Settings));
                var settings = (serializer.Deserialize(stream) as Settings) ?? new Settings();

                if (settings.IpAddress == "0.0.0.0" || settings.Username == "root" || settings.Password == "password")
                {
                    var message = "Default settings detected. Please update the config.xml with non-default values.";
                    if (settings.EnableLogging)
                    {
                        Logger.Instance.Log(message);
                    }
                    Console.WriteLine(message);
                    Environment.Exit(1);
                }

                return settings;
            }
            catch (Exception ex)
            {
                if (new Settings().EnableLogging) // Using default settings to determine if logging should occur
                {
                    Logger.Instance.Log("Error loading settings: " + ex.Message);
                }
                Console.WriteLine("Error loading settings: " + ex.Message);
                return new Settings();
            }
        }

        public void SaveSettings(Settings settings)
        {
            try
            {
                using var stream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write);
                var serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(stream, settings);
            }
            catch (Exception ex)
            {
                if (settings.EnableLogging)
                {
                    Logger.Instance.Log("Error saving settings: " + ex.Message);
                }
                Console.WriteLine("Error saving settings: " + ex.Message);
            }
        }
    }
}