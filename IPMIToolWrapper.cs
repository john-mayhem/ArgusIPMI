using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ArgusIPMI
{
    public class IPMIToolWrapper(string ipmitoolPath)
    {

        public async Task<string> GetSensorListAsync(string ipAddress, string username, string password)
        {
            Logger.Instance.Log("Getting sensor list.");
            string command = "sensor list";
            return await ExecuteCommandAsync(ipAddress, username, password, command);
        }

        public async Task<string> ExecuteCommandAsync(string ipAddress, string username, string password, string command)
        {
            Logger.Instance.Log($"Executing command: {command}");
            string result = string.Empty;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = ipmitoolPath,
                    Arguments = $"-I lanplus -H {ipAddress} -U {username} -P {password} {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                Logger.Instance.Log($"Starting process: {ipmitoolPath} with arguments: {startInfo.Arguments}");
                using var process = Process.Start(startInfo);

                if (process != null)
                {
                    using var reader = process.StandardOutput;
                    result = await reader.ReadToEndAsync();
                    Logger.Instance.Log($"Data Received!");
                    process.WaitForExit();
                }
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
            Logger.Instance.Log("Saving sensor data.");
            var dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath);
                Logger.Instance.Log("Created data directory.");
            }

            var filePath = Path.Combine(dataFolderPath, "data.txt");
            File.WriteAllText(filePath, data);
            Logger.Instance.Log($"Sensor data saved to {filePath}");
        }

        public static void ClearSensorData()
        {
            var dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataFolderPath)) Directory.CreateDirectory(dataFolderPath);

            var filePath = Path.Combine(dataFolderPath, "data.txt");
            File.WriteAllText(filePath, "");
            Logger.Instance.Log("Data file cleared.");
        }
    }
}
