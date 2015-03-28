using Advanced_Combat_Tracker;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace RainbowMage.ActServer
{
    class PluginMain : IActPluginV1
    {
        Server server;

        public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
        {
            ServicePointManager.DefaultConnectionLimit = 10;

            var builtinExtensions = LoadExtensionsFromAssembly(Assembly.GetExecutingAssembly());

            this.server = new Server(23456, "actserver");
            this.server.Extensions.AddRange(builtinExtensions);
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

        private IEnumerable<IExtension> LoadExtensionsFromAssembly(Assembly assembly)
        {
            var types = assembly.GetExportedTypes();
            var extensions = types.Where(t => t.GetInterface(typeof(IExtension).FullName) != null);
            foreach (var extension in extensions)
            {
                if (extension != typeof(ExtensionWrapper))
                {
                    if (extension.IsClass && !extension.IsAbstract && !extension.IsInterface)
                    {
                        var obj = assembly.CreateInstance(extension.FullName);
                        if (obj != null)
                        {
                            yield return new ExtensionWrapper(obj);
                        }
                    }
                }
            }
        }

        public IEnumerable<IExtension> GetBuiltInExtensions()
        {
            var result = new List<IExtension>();

            result.Add(new Extensions.MiniParseExtension());

            return result;
        }
    }
}
