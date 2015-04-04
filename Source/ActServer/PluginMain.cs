using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Microsoft.Owin.Hosting;
using Nancy.Hosting.Self;
using Nancy.Owin;
using Owin;
using RainbowMage.ActServer.Nancy;

namespace RainbowMage.ActServer
{
    public class PluginMain : IActPluginV1
    {
        AssemblyResolver resolver;
        ManualResetEvent initCompleteEvent;
        IDisposable owinHost;
        NancyHost nancyHost;
        Configuration config;
        TabPage tabPage;
        Label label;

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

            this.tabPage = pluginScreenSpace;
            this.label = pluginStatusText;

            Task.Run(() => Init());
        }

        public void DeInitPlugin()
        {
            if (initCompleteEvent.WaitOne())
            {
                SaveConfig();
                if (owinHost != null) owinHost.Dispose();
                if (nancyHost != null) nancyHost.Dispose();
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
                LoadConfig();
                InitializeConfigUI();

                var baseUri = string.Format("http://+:{0}/", config.Port);

                if (StartupOwinHost(baseUri)) { return; }

                baseUri = string.Format("http://localhost:{0}/", config.Port);
                StartupNancySelfHost(baseUri);
            }
            finally
            {
                initCompleteEvent.Set();
            }
        }

        private void LoadConfig()
        {
            try
            {
                var configFilePath = GetConfigFilePath();
                if (File.Exists(configFilePath))
                {
                    var xml = File.ReadAllText(configFilePath, Encoding.UTF8);
                    this.config = Configuration.FromXml(xml);
                }
                else
                {
                    this.config = new Configuration();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "Configuration load error: \n" + e.ToString(),
                    "Configuration load error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                MessageBox.Show("Creating new configuration.");

                this.config = new Configuration();
            }
        }

        private void SaveConfig()
        {
            var configFilePath = GetConfigFilePath();
            File.WriteAllText(configFilePath, this.config.ToXml(), Encoding.UTF8);
        }

        private void InitializeConfigUI()
        {
            var propertyGrid = new PropertyGrid();
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.SelectedObject = this.config;
            propertyGrid.PropertyValueChanged += (o, e) =>
            {
                var descriptor = e.ChangedItem.PropertyDescriptor;
                if (descriptor == null) return;

                // Get validator of the property and validate new value
                var validationAttrs = descriptor.Attributes.OfType<ValidationAttribute>();
                foreach (var validationAttr in validationAttrs)
                {
                    if (validationAttr.IsValid(e.ChangedItem.Value)) continue;

                    // Revert if value is not valid
                    descriptor.SetValue(propertyGrid.SelectedObject, e.OldValue);

                    // Show error message box if error message was set
                    if (string.IsNullOrEmpty(validationAttr.ErrorMessage))
                    {
                        MessageBox.Show(
                            validationAttr.ErrorMessage,
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    return;
                }

                // Warn if the property need to restart to take effect
                var restartAttr = descriptor.Attributes.OfType<RestartRequiredAttribute>().FirstOrDefault();
                if (restartAttr != null)
                {
                    var message = !string.IsNullOrEmpty(restartAttr.Message)
                        ? restartAttr.Message
                        : "You need to restart plugin for this change to take effect.";
                    MessageBox.Show(
                            message,
                            "Info",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);
                }
            };
            tabPage.Invoke(new Action(() => tabPage.Controls.Add(propertyGrid)));
        }

        private bool StartupOwinHost(string baseUri)
        {
            // Try startup
            if (TryStartupOwinHost(baseUri))
            {
                return true;
            }

            // When failed startup, configure namespace reservation
            if (UrlReservation.TryAdd(baseUri))
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
                owinHost = WebApp.Start(baseUri, app =>
                {
                    var configuration = new BootstrapParams()
                    {
                        RootDirectory = GetPluginDirectory(),
                        HostType = HostType.OwinSelfHost
                    };
                    var nancyOptions = new NancyOptions()
                    {
                        Bootstrapper = new Bootstrapper(configuration)
                    };

                    app.UseNancy(nancyOptions);
                });

                return true;
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is HttpListenerException)
                {
                    if (((HttpListenerException)e.InnerException).ErrorCode == 5) // Access denied
                    {
                        return false;
                    }
                }

                throw;
            }
        }

        private void StartupNancySelfHost(string baseUri)
        {
            var configuration = new BootstrapParams()
            {
                RootDirectory = GetPluginDirectory(),
                HostType = HostType.NancySelfHost
            };

            nancyHost = new NancyHost(
                new Uri(baseUri),
                new Bootstrapper(configuration),
                new HostConfiguration()
                {
                    UrlReservations = new UrlReservations() { CreateAutomatically = true },
                });

            nancyHost.Start();
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

        private static string GetConfigFilePath()
        {
            const string xmlFileName = "RainbowMage.ActServer.config.xml";

            if (ActGlobals.oFormActMain != null)
            {
                return Path.Combine(
                    ActGlobals.oFormActMain.AppDataFolder.FullName,
                    "Config",
                    xmlFileName);
            }
            else
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Advanced Combat Tracker",
                    "Config",
                    xmlFileName);
            }
        }
    }
}
