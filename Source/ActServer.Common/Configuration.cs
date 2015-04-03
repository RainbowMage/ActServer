using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer
{
    public interface IConfiguration
    {
        HostType HostType { get; }
    }

    public class Configuration : IConfiguration
    {
        public HostType HostType { get; set; }
    }

    public static class ConfigurationExtension
    {
        public static bool IsWebSocketAvailable(this IConfiguration config)
        {
            if (config.HostType == HostType.OwinSelfHost)
            {
                return true;
            }

            return false;
        }
    }
}
