using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Advanced_Combat_Tracker;
using Nancy;
using Nancy.Routing;
using RainbowMage.ActServer.Nancy;

namespace RainbowMage.ActServer.Modules
{
    public class MainModule : NancyModule
    {
        public MainModule(IBootstrapParams bootParams, IRouteCacheProvider route, ILog log)
        {
            log.Info("MainModule loaded.");

            this.After += context =>
            {
                if (context.Response != null)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
            };

            Get[@"/{dir?}"] = parameters =>
            {
                log.Debug("MainModule: Asset request from {0}: {1}", Request.UserHostAddress, Request.Path);
                string dir = parameters.dir;
                var path = Join(bootParams.AssetDirectoryName, dir, "index.html");
                return Response.AsFile(path, "text/html");
            };

            Get["/command/version"] = _ =>
            {
                log.Debug("MainModule: Version command from {0}", Request.UserHostAddress, Request.Path);
                var asm = typeof(PluginMain).Assembly;
                var modules = route.GetCache().Select(x => new
                {
                    TypeName = x.Key.FullName,
                    AssemblyName = x.Key.Assembly.FullName,
                    Version = x.Key.Assembly.GetName().Version.ToString(),
                    Copyright = GetAssemblyCopyright(x.Key.Assembly),
                    Routes = x.Value.Select(y => string.Format("{0}:{1}", y.Item2.Method, y.Item2.Path))
                });

                return Response.AsJson(new
                {
                    Name = asm.GetName().Name,
                    Version = asm.GetName().Version.ToString(),
                    Copyright = GetAssemblyCopyright(asm),
                    RootDirectory = bootParams.RootDirectory,
                    HostType = bootParams.HostType.ToString(),
                    Modules = modules
                });
            };
        }

        private static IEnumerable<Type> EnumerateNancyModules()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes()             // Enumerate all types
                    .Where(x => typeof(NancyModule).IsAssignableFrom(x) // which inherit from NancyModule class
                                && !x.FullName.StartsWith("Nancy.")));  // except "Nancy" namespace.
        }

        private static string GetAssemblyCopyright(Assembly assembly)
        {
            var copyrightAttrs = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
            return copyrightAttrs.Any()
                ? ((AssemblyCopyrightAttribute)copyrightAttrs.First()).Copyright
                : "";
        }

        private static string Join(string asset, string dir, string index)
        {
            var result = "";

            if (!string.IsNullOrEmpty(asset)) result += asset + "/";
            if (!string.IsNullOrEmpty(dir)) result += dir += "/";
            result += index;

            return result;
        }
    }
}
