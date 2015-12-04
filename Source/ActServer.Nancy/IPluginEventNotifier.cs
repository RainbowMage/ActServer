using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Nancy
{
    interface IPluginEventNotifier
    {
        event EventHandler DeInitializing;
    }

    class PluginEventNotifier : IPluginEventNotifier
    {
        public event EventHandler DeInitializing;
    }
}
