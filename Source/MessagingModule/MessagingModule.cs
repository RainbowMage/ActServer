using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using RainbowMage.ActServer;
using RainbowMage.ActServer.Nancy;

namespace MessagingModule
{
    public class MessagingModule : NancyModule
    {
        private const int MessageMaxQueue = 20;

        private static Queue<Message> messageQueue;
        private static ulong latestMessageTimestamp;
        private static event EventHandler<MessageReceivedEventArgs> MessageReceived;

        static MessagingModule()
        {
            messageQueue = new Queue<Message>();
        }

        public MessagingModule(IBootstrapParams bootParams)
        {
            this.After += context =>
            {
                if (context.Response != null)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
            };

            if (bootParams.IsWebSocketAvailable())
            {
                Get["/websocket/message"] = _ =>
                {
                    string clientName = Request.Query.name;
                    if (string.IsNullOrEmpty(clientName))
                    {
                        return Response.AsJsonErrorMessage("Missing parameter 'name'.");
                    }

                    var webSocket = new NancyWebSocket();
                    webSocket.Received += async (o, e) =>
                    {
                        var text = Encoding.UTF8.GetString(e.Message);
                        var message = Message.FromJson(text, true);
                        if (message == null)
                        {
                            await ((NancyWebSocket)o).SendTextAsync("Unrecognizable message format.");
                            return;
                        }

                        if (string.IsNullOrEmpty(message.From))
                        {
                            await ((NancyWebSocket)o).SendTextAsync("Missing parameter 'from'.");
                            return;
                        }

                        EnqueueMessage(message);
                    };

                    var handler = new EventHandler<MessageReceivedEventArgs>(async (o, e) =>
                    {
                        if (e.Message.IsBroadcast || e.Message.To == clientName)
                        {
                            await webSocket.SendTextAsync(e.Message.GetJson());
                        }
                    });

                    MessageReceived += handler;
                    webSocket.Disposed += (o, e) => MessageReceived -= handler;

                    return webSocket.AcceptWebSocketRequest(Context);
                };
            }

            Get["/command/message/receive"] = _ =>
            {
                string clientName = Request.Query.name;
                if (string.IsNullOrEmpty(clientName))
                {
                    return Response.AsJsonErrorMessage("Missing parameter 'name'.");
                }

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

            Get["/command/message/send"] = _ =>
            {
                string from = Request.Query.from;
                if (string.IsNullOrEmpty(from))
                {
                    return Response.AsJsonErrorMessage("Missing parameter 'from'.");
                }
                string to = Request.Query.to;
                var isBroadcast = string.IsNullOrEmpty(to);
                string body = Request.Query.body ?? "";

                var timestamp = Message.GetCurrentTimestamp();
                var message = new Message(from, to, body, isBroadcast, timestamp);

                EnqueueMessage(message);

                return Response.AsEmptyJson();
            };
        }

        private void EnqueueMessage(Message message)
        {
            lock (messageQueue)
            {
                messageQueue.Enqueue(message);

                if (messageQueue.Count > MessageMaxQueue)
                {
                    messageQueue.Dequeue();
                }

                latestMessageTimestamp = message.Timestamp;
            }

            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageReceivedEventArgs(message));
            }
        }

        class MessageReceivedEventArgs : EventArgs
        {
            public Message Message { get; private set; }

            public MessageReceivedEventArgs(Message message)
            {
                this.Message = message;
            }
        }
    }
}
