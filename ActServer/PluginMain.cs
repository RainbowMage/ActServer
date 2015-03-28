using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer
{
    class PluginMain : IActPluginV1
    {
        Server server;

        public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
        {
            ServicePointManager.DefaultConnectionLimit = 10;

            var miniParseExtension = new Extensions.MiniParseExtension();

            this.server = new Server(23456);
            this.server.Extensions.AddRange(GetBuiltInExtensions());
            this.server.Extensions.AddRange(LoadExtensions());
            this.server.Start();
        }

        public void DeInitPlugin()
        {
            this.server.Stop();

            foreach (var extension in this.server.Extensions)
            {
                extension.Dispose();
            }
        }

        public IEnumerable<IExtension> LoadExtensions()
        {
            var result = new List<IExtension>();

            return result;
        }

        public IEnumerable<IExtension> GetBuiltInExtensions()
        {
            var result = new List<IExtension>();

            result.Add(new Extensions.MiniParseExtension());

            return result;
        }
    }
}
