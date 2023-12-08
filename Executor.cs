using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ArgusIPMI
{
    // https://www.youtube.com/watch?v=aoPNKphgawc
    // 
    // Checking the current third party/non-Dell cooling profile: 
    // .\ipmitool -I lanplus -H ipaddress -U root -P password raw 0x30 0xce 0x01 0x16 0x05 0x00 0x00 0x00 
    // 
    // Disabled Output (Quiet Fans) 
    // 16 05 00 00 00 05 00 01 00 00  
    // 
    // Enabled Output (Noisy Fans) 
    // 16 05 00 00 00 05 00 00 00 00  
    // 
    // Disable the third party/non-Dell cooling profile: 
    // .\ipmitool -I lanplus -H ipaddress -U root -P password raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 0x01 0x00 0x00 
    // 
    // Enable the third party/non-Dell cooling profile: 
    // .\ipmitool -I lanplus -H ipaddress -U root -P password raw 0x30 0xce 0x00 0x16 0x05 0x00 0x00 0x00 0x05 0x00 0x00 0x00 0x00 


    public class Executor(IPMIToolWrapper ipmiToolWrapper, Settings settings)
    {
        private readonly IPMIToolWrapper _ipmiToolWrapper = ipmiToolWrapper;
        private readonly Settings _settings = settings;

        public async Task SetIPMIMode(bool automatic)
        {
            string command = automatic ?
                "raw 0x30 0x30 0x01 0x01" : // Automatic command
                "raw 0x30 0x30 0x01 0x00"; // Manual command

            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }

        public async Task SetFanSpeed10()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x0a"; // Command for 10%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }

        public async Task SetFanSpeed20()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x14"; // Command for 20%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }
        public async Task SetFanSpeed30()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x1e"; // Command for 30%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }

        public async Task SetFanSpeed40()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x28"; // Command for 40%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }

        public async Task SetFanSpeed50()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x32"; // Command for 50%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }
        public async Task SetFanSpeed60()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x3c"; // Command for 60%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }
        public async Task SetFanSpeed70()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x46"; // Command for 70%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }

        public async Task SetFanSpeed80()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x50"; // Command for 80%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }
        public async Task SetFanSpeed90()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x5a"; // Command for 90%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }

        public async Task SetFanSpeed100()
        {
            string command = "raw 0x30 0x30 0x02 0xff 0x64"; // Command for 100%
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }

        public async Task SetFanSpeed(string hexSpeed)
        {
            string command = $"raw 0x30 0x30 0x02 0xff {hexSpeed}";
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }

    }
}
