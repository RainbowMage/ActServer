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
using Advanced_Combat_Tracker;
using Microsoft.Owin.Hosting;
using Nancy.Hosting.Self;
using RainbowMage.ActServer.Nancy;

namespace RainbowMage.ActServer
{
    public class PluginMain : IActPluginV1
    {
        AssemblyResolver resolver;
        ManualResetEvent initCompleteEvent;
        IDisposable owinHost;

        public PluginMain()
        {
            initCompleteEvent = new ManualResetEvent(false);
            initCompleteEvent.Set();
        }

        #region IActPluginV1

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            initCompleteEvent.Reset();

            resolver = new AssemblyResolver(new Dictionary<string, bool>
            {
                { GetLibraryDirectory(), true },
                { GetPluginDirectory(), false },
                { GetModuleDirectory(), true }
            });

            Task.Run(() => Init());
        }

        public void DeInitPlugin()
        {
            if (initCompleteEvent.WaitOne())
            {
                if (owinHost != null) owinHost.Dispose();
                if (resolver != null) resolver.Dispose();
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Init()
        {
            try
            {
                LoadModules();
                const int port = 23456;
                var baseUri = string.Format("http://+:{0}/", port);

                if (StartupOwinHost(baseUri)) { return; }

                baseUri = string.Format("http://localhost:{0}/", port);
                StartupNancySelfHost(baseUri);
            }
            finally
            {
                initCompleteEvent.Set();
            }
        }

        private bool StartupOwinHost(string baseUri)
        {
            // Try startup
            if (TryStartupOwinHost(baseUri))
            {
                return true;
            }

            // When failed startup, configure namespace reservation
            if (UrlReservations.TryAdd(baseUri))
            {
                // Try startup again
                if (TryStartupOwinHost(baseUri))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryStartupOwinHost(string baseUri)
        {
            try
            {
                owinHost = WebApp.Start<OwinStartup>(baseUri);

                return true;
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is HttpListenerException)
                {
                    if (((HttpListenerException) e.InnerException).ErrorCode == 5) // Access denied
                    {
                        return false;
                    }
                }

                throw;
            }
        }

        private void StartupNancySelfHost(string baseUri)
        {
            var configuration = new Configuration()
            {
                HostType = HostType.NancySelfHost
            };

            var host = new NancyHost(
                new Uri(baseUri),
                new Bootstrapper(configuration),
                new HostConfiguration()
                {
                    UrlReservations = new global::Nancy.Hosting.Self.UrlReservations() { CreateAutomatically = true },
                });

            host.Start();
        }

        public void LoadModules()
        {
            var moduleDirectory = GetModuleDirectory();
            if (!Directory.Exists(moduleDirectory))
            {
                return;
            }
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

        private string GetPluginDirectory()
        {
            if (ActGlobals.oFormActMain != null)
            {
                var plugin = ActGlobals.oFormActMain.ActPlugins.FirstOrDefault(x => x.pluginObj == this);
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

        private string GetLibraryDirectory()
        {
            return Path.Combine(GetPluginDirectory(), "libraries");
        }

        private string GetModuleDirectory()
        {
            return Path.Combine(GetPluginDirectory(), "modules");
        }
    }
}
