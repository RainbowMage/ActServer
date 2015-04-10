using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace GameLogModule
{
    [DataContract]
    class GameLog
    {
        private static readonly DataContractJsonSerializer JsonSerializer =
            new DataContractJsonSerializer(typeof(GameLog));

        [DataMember(Name = "time")]
        public ulong Time { get; private set; }
        [DataMember(Name = "type")]
        public int Type { get; private set; }
        [DataMember(Name = "zone")]
        public string Zone { get; private set; }
        [DataMember(Name = "combat")]
        public bool IsCombat { get; private set; }
        [DataMember(Name = "text")]
        public string Text { get; private set; }

        public GameLog(DateTime time, int type, string zone, bool combat, string text)
        {
            this.Time = GetTimestamp(time);
            this.Type = type;
            this.Zone = zone;
            this.IsCombat = combat;
            this.Text = text;
        }

        private string jsonCache;

        public string GetJson()
        {
            if (jsonCache != null) return jsonCache;

            using (var stream = new MemoryStream())
            {
                JsonSerializer.WriteObject(stream, this);
                jsonCache = Encoding.UTF8.GetString(stream.ToArray());
                return jsonCache;
            }
        }

        private static ulong GetTimestamp(DateTime time)
        {
            return (ulong)(time.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}
