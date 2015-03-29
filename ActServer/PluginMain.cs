using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace RainbowMage.ActServer
{
    class PluginMain : IActPluginV1
    {
        Server server;

        public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
        {
            ServicePointManager.DefaultConnectionLimit = 10;

            var extensions = LoadExtensions();

            this.server = new Server(23456, "actserver");
            this.server.Extensions.AddRange(extensions);
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

            foreach (var file in System.IO.Directory.GetFiles(GetPluginDirectory(), "*.dll"))
            {
                var assembly = Assembly.LoadFrom(file);
                var extensions = LoadExtensionsFromAssembly(assembly);
                foreach (var extension in extensions)
                {
                    Console.WriteLine("Plugin: {0} ({1})", extension.DisplayName, extension.ExtensionName);
                    yield return extension;
                }
            }
        }

        private static Assembly LoadAssembly(string asmPath)
        {
            if (System.IO.File.Exists(asmPath))
            {
                var pdbPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(asmPath),
                    System.IO.Path.GetFileNameWithoutExtension(asmPath) + ".pdb");

                byte[] asmData = System.IO.File.ReadAllBytes(asmPath);
                byte[] pdbData = null;
                if (System.IO.File.Exists(pdbPath))
                {
                    pdbData = System.IO.File.ReadAllBytes(pdbPath);
                }

                return AppDomain.CurrentDomain.Load(asmData, pdbData);
            }
            else
            {
                throw new System.IO.FileNotFoundException();
            }
        }

        private IEnumerable<IExtension> LoadExtensionsFromAssembly(Assembly assembly)
        {
            var types = assembly.GetExportedTypes();
            var extensions = types.Where(t => t.GetInterface(typeof(IExtension).FullName) != null);
            foreach (var extension in extensions)
            {
                if (extension.FullName != typeof(ExtensionWrapper).FullName)
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

        private string GetPluginDirectory()
        {
            if (ActGlobals.oFormActMain != null)
            {
                var plugin = ActGlobals.oFormActMain.ActPlugins.Where(x => x.pluginObj == this).FirstOrDefault();
                if (plugin != null)
                {
                    return System.IO.Path.GetDirectoryName(plugin.pluginFile.FullName);
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
    }
}
