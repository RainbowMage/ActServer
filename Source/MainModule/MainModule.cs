using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Nancy;
using Nancy.Routing;
using RainbowMage.ActServer.Nancy;

namespace RainbowMage.ActServer.Modules
{
    public class MainModule : NancyModule
    {
        public MainModule(IBootstrapParams bootParams, IRouteCacheProvider route)
        {
            this.After += context =>
            {
                if (context.Response != null)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
            };

            Get["/command/version"] = _ =>
            {
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
    }
}
