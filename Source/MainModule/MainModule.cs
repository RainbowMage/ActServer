using RainbowMage.ActServer;
using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace RainbowMage.ActServer.Modules
{
    public class MainModule : NancyModule
    {
        private static Queue<MessageEntry> messageQueue;
        private static ulong latestMessageTimestamp;

        static MainModule()
        {
            messageQueue = new Queue<MessageEntry>();
        }

        public MainModule()
        {
            this.After += context =>
            {
                if (context.Response != null)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
            };


            Get["/message/receive"] = _ =>
            {
                string clientName = Request.Query.name;
                string timestampString = Request.Query.timestamp;
                ulong timestamp;
                if (ulong.TryParse(timestampString, out timestamp))
                {
                    while (true)
                    {
                        if (latestMessageTimestamp > timestamp)
                        {
                            var message = messageQueue.FirstOrDefault(
                                x => x.Timestamp > timestamp
                                     && (x.IsBroadcast || x.To == clientName));
                            if (message != null)
                            {
                                return Response.AsJson(message);
                            }
                        }
                        Thread.Sleep(50);
                    }
                }
                else
                {
                    return Response.AsJsonErrorMessage(string.Format("Could not parse timestamp '{0}'.", timestampString));
                }
            };

            Get["/message/send"] = _ =>
            {
                string from = Request.Query.from;
                if (string.IsNullOrEmpty(from))
                {
                    return Response.AsJsonErrorMessage("Missing parameter 'from'.");
                }
                string to = Request.Query.to;
                var isBroadcast = string.IsNullOrEmpty(to);
                string message = Request.Query.message;
                if (message == null)
                {
                    return Response.AsJsonErrorMessage("Missing parameter 'message'.");
                }

                var timestamp = (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
                var messageEntry = new MessageEntry(from, to, message, isBroadcast, timestamp);
                messageQueue.Enqueue(messageEntry);

                if (messageQueue.Count > 20)
                {
                    messageQueue.Dequeue();
                }

                latestMessageTimestamp = timestamp;

                return Response.AsEmptyJson();
            };

            Get["/"] = Get["/version"] = _ =>
            {
                var asm = typeof(RainbowMage.ActServer.PluginMain).Assembly;
                
                var modules = EnumerateNancyModules().Select(m => new
                {
                    Name = m.FullName,
                    Version = m.Assembly.GetName().Version.ToString(),
                    Copyright = GetAssemblyCopyright(m.Assembly)
                });

                return Response.AsJson(new
                {
                    Name = asm.GetName().Name,
                    Version = asm.GetName().Version.ToString(),
                    Copyright = GetAssemblyCopyright(asm),
                    Modules = modules
                });
            };
        }

        private IEnumerable<Type> EnumerateNancyModules()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var nancyModules = assembly.GetTypes()
                    .Where(x => typeof(Nancy.NancyModule).IsAssignableFrom(x)
                    && !x.FullName.StartsWith("Nancy."));
                foreach (var module in nancyModules)
                {
                    yield return module;
                }
            }
        }

        private string GetAssemblyCopyright(Assembly assembly)
        {
            var copyrightAttrs = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyCopyrightAttribute), true);
            return copyrightAttrs.Any()
                ? ((System.Reflection.AssemblyCopyrightAttribute)copyrightAttrs.First()).Copyright
                : "";
        }
    }
}
