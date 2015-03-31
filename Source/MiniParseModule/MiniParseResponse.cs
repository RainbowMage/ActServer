using System;
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
    class MiniParseResponse
    {
        private static readonly DataContractJsonSerializer jsonSerializer =
               new DataContractJsonSerializer(typeof(MiniParseResponse), new DataContractJsonSerializerSettings()
               {
                   UseSimpleDictionaryFormat = true
               });

        [DataMember(Name = "Encounter")]
        public Dictionary<string, string> Encounter { get; set; }

        [DataMember(Name = "Combatant")]
        public OrderedDictionary<string, Dictionary<string, string>> Combatant { get; set; }

        [DataMember(Name = "isActive")]
        public bool IsActive { get; set; }

        [DataMember(Name = "processingTime", EmitDefaultValue = false)]
        public double? ProcessingTime { get; set; }

        public string GetJson()
        {
            using (var stream = new MemoryStream())
            {
                jsonSerializer.WriteObject(stream, this);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
