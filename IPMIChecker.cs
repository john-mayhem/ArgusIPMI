using System;
using System.Threading.Tasks;

namespace ArgusIPMI
{
    public class IPMIChecker
    {
        public static async Task<bool> TryIPMIConnection(IPMIToolWrapper ipmiWrapper, Settings currentSettings)
        {
            if (currentSettings == null || ipmiWrapper == null)
            {
                Logger.Instance.Log("Settings or IPMIToolWrapper are null. Cannot attempt IPMI connection.");
                return false;
            }

            try
            {
                var sensorData = await ipmiWrapper.GetSensorListAsync(currentSettings.IpAddress, currentSettings.Username, currentSettings.Password);

                if (sensorData.Contains("Error:"))
                {
                    Logger.Instance.Log("IPMI Connection Error: Unable to establish session.");
                    Console.WriteLine("IPMI Connection Error: Unable to establish session.");
                    IPMIToolWrapper.ClearSensorData();
                    return false;
                }
                else if (sensorData.Contains("Error:"))
                {
                    Logger.Instance.Log("IPMI Connection Error: Incorrect password.");
                    Console.WriteLine("IPMI Connection Error: Incorrect password.");
                    IPMIToolWrapper.ClearSensorData();
                    return false;
                }

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
