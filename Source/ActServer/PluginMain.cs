using Advanced_Combat_Tracker;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RainbowMage.ActServer
{
    public class PluginMain : IActPluginV1
    {
        AssemblyResolver resolver;
        NancyHost nancyHost;
        AutoResetEvent initCompleteEvent;
        Task initTask;

        static PluginMain()
        {
            
        }

        public PluginMain()
        {
            initCompleteEvent = new AutoResetEvent(false);
        }

        public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
        {
            initCompleteEvent = new AutoResetEvent(false);

            var searchPath = new Dictionary<string, bool>
            {
                { GetPluginDirectory(), false },
                { GetModuleDirectory(), true }
            };

            resolver = new AssemblyResolver(searchPath);

            initTask = Task.Run(() => Init());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Init()
        {
            LoadModules();

            var config = new HostConfiguration()
            {
                UrlReservations = new UrlReservations() { CreateAutomatically = true }
            };

            var port = 23456;
            var nancyHost = new NancyHost(config, new Uri(string.Format("http://localhost:{0}/", port)));
            nancyHost.Start();
            this.nancyHost = nancyHost;

            initCompleteEvent.Set();
        }

        public void DeInitPlugin()
        {
            if (initCompleteEvent.WaitOne())
            {
                nancyHost.Dispose();
                resolver.Dispose();
            }
        }

        public void LoadModules()
        {
            var moduleDirectory = GetModuleDirectory();
            if (Directory.Exists(moduleDirectory))
            {
                foreach (var file in Directory.GetFiles(moduleDirectory, "*.dll", SearchOption.AllDirectories))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0}: {1}", file, e);
                    }
                }
            }
        }

        private string GetPluginDirectory()
        {
            if (ActGlobals.oFormActMain != null)
            {
                var plugin = ActGlobals.oFormActMain.ActPlugins.Where(x => x.pluginObj == this).FirstOrDefault();
                if (plugin != null)
                {
                    return Path.GetDirectoryName(plugin.pluginFile.FullName);
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                return Application.StartupPath;
            }
        }

        private string GetModuleDirectory()
        {
            return Path.Combine(GetPluginDirectory(), "modules");
        }
    }
}
