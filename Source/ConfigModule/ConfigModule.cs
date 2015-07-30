using Nancy;
using RainbowMage.ActServer.Nancy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ConfigModule
{
    public class ConfigModule : NancyModule
    {
        const string ConfigFileName = "RainbowMage.ActServer.ConfigModule.config.json";
        static readonly DataContractJsonSerializer Serializer = 
            new DataContractJsonSerializer(typeof(ConcurrentDictionary<string, string>));

        ConcurrentDictionary<string, string> config;

        public ConfigModule(IBootstrapParams bootParams, ILog log)
        {
            log.Info("ConfigModule loaded.");
            
            Load(bootParams.ConfigDirectory);

            this.After += context =>
            {
                if (context.Response != null)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
            };

            Get["/command/getconfig"] = _ =>
            {
                string name = Request.Query.name;
                if (config.ContainsKey(name))
                {
                    return config[name];
                }
                else
                {
                    return "";
                }
            };

            Get["/command/setconfig"] = _ =>
            {
                string name = Request.Query.name;
                string value = Request.Query.value;
                if (config.ContainsKey(name))
                {
                    return config[name] = value;
                }
                else
                {
                    return config;
                }
            };
        }

        private static string GetConfigFileName(string directory)
        {
            return System.IO.Path.Combine(directory, ConfigFileName);
        }

        private void Load(string directory)
        {
            var fileName = GetConfigFileName(directory);
            try
            {
                if (System.IO.File.Exists(fileName))
                {
                    using (var stream = System.IO.File.OpenRead(GetConfigFileName(directory)))
                    {
                        config = Serializer.ReadObject(stream) as ConcurrentDictionary<string, string>;
                    }
                }
            }
            catch
            { }

            if (config == null)
            {
                config = new ConcurrentDictionary<string, string>();
            }
        }

        private void Save(string directory)
        {
            using (var stream = System.IO.File.OpenWrite(GetConfigFileName(directory)))
            {
                Serializer.WriteObject(stream, config);
            }
        }
    }
}
