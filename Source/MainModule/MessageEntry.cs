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
    class MessageEntry
    {
        private static readonly DataContractJsonSerializer jsonSerializer =
            new DataContractJsonSerializer(typeof(MessageEntry));

        [DataMember(Name = "from")]
        public string From { get; private set; }
        [DataMember(Name = "to")]
        public string To { get; private set; }
        [DataMember(Name = "message")]
        public string Message { get; private set; }
        [DataMember(Name = "isBroadcast")]
        public bool IsBroadcast { get; private set; }
        [DataMember(Name = "timestamp")]
        public ulong Timestamp { get; private set; }

        public MessageEntry(string from, string to, string message, bool isBroadcast, ulong timestamp)
        {
            this.From = from;
            this.To = to;
            this.Message = message;
            this.IsBroadcast = isBroadcast;
            this.Timestamp = timestamp;
        }

        private string jsonCache;

        public string GetJson()
        {
            if (jsonCache != null) return jsonCache;

            using (var stream = new MemoryStream())
            {
                jsonSerializer.WriteObject(stream, this);
                jsonCache = Encoding.UTF8.GetString(stream.ToArray());
                return jsonCache;
            }
        }
    }
}
