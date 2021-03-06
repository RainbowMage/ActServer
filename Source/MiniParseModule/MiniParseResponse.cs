﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Modules
{
    [DataContract]
    public class MiniParseResponse
    {
        private static readonly DataContractJsonSerializer JsonSerializer =
               new DataContractJsonSerializer(typeof(MiniParseResponse), new DataContractJsonSerializerSettings()
               {
                   UseSimpleDictionaryFormat = true
               });

        [DataMember(Name = "encounter")]
        public Dictionary<string, string> Encounter { get; set; }

        [DataMember(Name = "combatants")]
        public OrderedDictionary<string, Dictionary<string, string>> Combatant { get; set; }

        [DataMember(Name = "isActive")]
        public bool IsActive { get; set; }

        [DataMember(Name = "processingTime", EmitDefaultValue = false)]
        public double? ProcessingTime { get; set; }

        public string GetJson()
        {
            using (var stream = new MemoryStream())
            {
                JsonSerializer.WriteObject(stream, this);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
