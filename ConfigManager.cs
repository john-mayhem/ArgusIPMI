using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace ArgusIPMI
{
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
                var settings = (serializer.Deserialize(stream) as Settings) ?? new Settings();

                // Check if any settings are default
                if (settings.IpAddress == "0.0.0.0" || settings.Username == "change-me" || settings.Password == "change-me")
                {
                    var message = "Default settings detected. Please update the config.xml with non-default values.";
                    Logger.Instance.Log(message);
                    Console.WriteLine(message);
                    Environment.Exit(1); // Exit the application
                }

                return settings;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Error loading settings: " + ex.Message);
                Console.WriteLine("Error loading settings: " + ex.Message);
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
}