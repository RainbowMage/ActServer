using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RainbowMage.ActServer
{
    [DataContract]
    public class Configuration
    {
        private static readonly DataContractSerializer Serializer =
            new DataContractSerializer(typeof(Configuration));

        [DataMember(Name = "Port")]
        [Range(0, 65535)]
        [Description("Port number for the server. (0 - 65535)")]
        [RestartRequired]
        public int Port { get; set; }

        [DataMember(Name = "Version")]
        [RegularExpression(@"\d+\.\d+\.\d+\.\d+")]
        [Browsable(false)]
        public string Version { get; private set; }

        public Configuration()
        {
            this.Port = 23456;
            this.Version = GetVersion();
        }

        public string ToXml()
        {
            // Set application version before save
            this.Version = GetVersion();

            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8
            };

            var stringBuilder = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                Serializer.WriteObject(xmlWriter, this);
            }

            return stringBuilder.ToString();
        }

        public static Configuration FromXml(string xml)
        {
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                return Serializer.ReadObject(memoryStream) as Configuration;
            }
        }

        private static string GetVersion()
        {
            return typeof(Configuration).Assembly.GetName().Version.ToString();
        }
    }
}
