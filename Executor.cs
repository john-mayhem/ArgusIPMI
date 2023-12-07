using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ArgusIPMI
{

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

        public async Task SetFanSpeed(string hexSpeed)
        {
            string command = $"raw 0x30 0x30 0x02 0xff {hexSpeed}";
            await _ipmiToolWrapper.ExecuteCommandAsync(_settings.IpAddress, _settings.Username, _settings.Password, command);
        }
    }
}
