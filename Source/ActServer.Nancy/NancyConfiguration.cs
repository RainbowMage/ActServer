using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Nancy
{
    public interface IBootstrapParams
    {
        string RootDirectory { get; }
        HostType HostType { get; }
    }

    public class BootstrapParams : IBootstrapParams
    {
        public string RootDirectory { get; set; }
        public HostType HostType { get; set; }
    }

    public static class ConfigurationExtension
    {
        public static bool IsWebSocketAvailable(this IBootstrapParams info)
        {
            if (info.HostType == HostType.OwinSelfHost)
            {
                return true;
            }

            return false;
        }
    }
}
