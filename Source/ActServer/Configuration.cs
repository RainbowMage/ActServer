using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms.Design;
using System.Xml;

namespace RainbowMage.ActServer
{
    [DataContract]
    public class Configuration
    {
        private static readonly DataContractSerializer Serializer =
            new DataContractSerializer(typeof(Configuration));

        [DataMember(Name = "Port")]
        [Required]
        [Range(0, 65535)]
        [Description("Port number for the server. (0 - 65535)")]
        [RestartRequired]
        public int Port { get; set; }

        [DataMember(Name = "RootPath")]
        [Required]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        [RestartRequired("You need to restart ACT (not just plugin) for this change to take effect.")]
        public string RootPath { get; set; }

        [DataMember(Name = "AssetDirectoryName")]
        [RestartRequired("You need to restart ACT (not just plugin) for this change to take effect.")]
        public string AssetDirectoryName { get; set; }

        [DataMember(Name = "Version")]
        [RegularExpression(@"\d+\.\d+\.\d+\.\d+")]
        [Browsable(false)]
        public string Version { get; private set; }

        public Configuration()
        {
            this.Port = 23456;
            this.Version = GetVersion();
        }

        class StringWriterEx : StringWriter
        {
            Encoding encoding;

            public StringWriterEx(Encoding encoding)
            {
                this.encoding = encoding;
            }

            public override Encoding Encoding
            {
                get { return this.encoding; }
            }
        }

        public string ToXml()
        {
            // Set application version before save
            this.Version = GetVersion();

            var settings = new XmlWriterSettings
            {
                Indent = true
            };

            var stringWriter = new StringWriterEx(Encoding.UTF8);
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                Serializer.WriteObject(xmlWriter, this);
            }

            return stringWriter.ToString();
        }

        public static Configuration FromXml(string xml)
        {
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var config = Serializer.ReadObject(memoryStream) as Configuration;

                if (config == null)
                {
                    throw new SerializationException("Deserialization failed.");
                }

                // Validate configuration values
                var context = new ValidationContext(config, null, null);
                var results = new List<ValidationResult>();
                var isValid = Validator.TryValidateObject(config, context, results, true);
                if (!isValid)
                {
                    throw new ValidationException("Configuration values is not valid.");
                }

                return config;
            }
        }

        private static string GetVersion()
        {
            return typeof(Configuration).Assembly.GetName().Version.ToString();
        }
    }
}
