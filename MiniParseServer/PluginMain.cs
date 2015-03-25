using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.MiniParseServer
{
    class PluginMain : IActPluginV1
    {
        Server server;

        public void DeInitPlugin()
        {
            this.server.Stop();
        }

        public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
        {
            this.server = new Server(23456);
            this.server.Start();
        }

    }
}
