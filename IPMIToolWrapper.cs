﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace ArgusIPMI
{
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
        }

        public static void ClearSensorData(string data)
        {
            var dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataFolderPath)) Directory.CreateDirectory(dataFolderPath);

            var filePath = Path.Combine(dataFolderPath, "data.txt"); // Fixed file name
            File.WriteAllText(filePath, ""); // Overwrite the file with new data
            Logger.Instance.Log("Data file cleared.");
        }
    }
}