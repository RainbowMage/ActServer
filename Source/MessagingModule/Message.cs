using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace MessagingModule
{
    [DataContract]
    class Message
    {
        private static readonly DataContractJsonSerializer JsonSerializer =
            new DataContractJsonSerializer(typeof(Message));

        [DataMember(Name = "from")]
        public string From { get; private set; }
        [DataMember(Name = "to")]
        public string To { get; private set; }
        [DataMember(Name = "body")]
        public string Body { get; private set; }
        [DataMember(Name = "isBroadcast")]
        public bool IsBroadcast { get; private set; }
        [DataMember(Name = "timestamp")]
        public ulong Timestamp { get; private set; }

        public Message(string from, string to, string body, bool isBroadcast, ulong timestamp)
        {
            this.From = from;
            this.To = to;
            this.Body = body;
            this.IsBroadcast = isBroadcast;
            this.Timestamp = timestamp;
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

        public static Message FromJson(string json, bool setTimestamp = false)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    var data = Encoding.UTF8.GetBytes(json);
                    stream.Write(data, 0, data.Length);
                    stream.Position = 0;
                    var message = JsonSerializer.ReadObject(stream) as Message;
                    if (message != null)
                    {
                        message.IsBroadcast = string.IsNullOrEmpty(message.To);
                        if (setTimestamp)
                        {
                            message.Timestamp = GetCurrentTimestamp();
                        }
                    }

                    return message;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ulong GetCurrentTimestamp()
        {
            return (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}
