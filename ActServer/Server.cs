using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RainbowMage.ActServer
{
    public class Server
    {
        private HttpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private bool running = false;
        private int port;
        private string serviceName;
        private Queue<MessageEntry> messageQueue;
        private ulong latestMessageTimestamp;

        public List<IExtension> Extensions { get; set; }

        public Server(int port, string serviceName)
        {
            this.port = port;
            this.serviceName = serviceName;
            this.messageQueue = new Queue<MessageEntry>();
            this.Extensions = new List<IExtension>();
        }

        public void Start()
        {
            if (this.running)
            {
                return;
            }

            this.running = true;
            this.cancellationTokenSource = new CancellationTokenSource();

            this.listener = new HttpListener();
            var prefixFormat = "http://+:{0}/{1}/";
            if (serviceName.Trim() == string.Empty)
            {
                prefixFormat = "http://+:{0}/";
            }
            this.listener.Prefixes.Add(string.Format(prefixFormat, port, serviceName));
            this.listener.Start();

            var token = cancellationTokenSource.Token;
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    var result = this.listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
                    result.AsyncWaitHandle.WaitOne();
                }
            }, token);
        }

        private void ListenerCallback(IAsyncResult result)
        {
            try
            {
                var listener = (HttpListener)result.AsyncState;
                if (listener.IsListening)
                {
                    var context = listener.EndGetContext(result);
                    Console.WriteLine("[{0}] Receive: {1}", context.Request.RequestTraceIdentifier, context.Request.RawUrl);

                    var actionName = context.Request.QueryString.Get("action");
                    if (actionName == string.Empty)
                    {
                        SendErrorResponse(context, "Action name is not specified.", actionName);
                    } 
                    else if (actionName == "requestData")
                    {
                        OnRequestData(context);
                    }
                    else if (actionName == "requestMessage")
                    {
                        OnRequestMessage(context);
                    }
                    else if (actionName == "sendMessage")
                    {
                        OnReceiveMessage(context, false);
                    }
                    else if (actionName == "broadcastMessage")
                    {
                        OnReceiveMessage(context, true);
                    }
                    else
                    {
                        SendErrorResponse(context, "Action name '{0}' is not implemented.", actionName);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        private void OnRequestData(HttpListenerContext context)
        {
            var extensionName = context.Request.QueryString.Get("dataType");
            var extension = this.Extensions.FirstOrDefault(x => x.ExtensionName == extensionName);
            if (extension != null)
            {
                extension.ProcessRequest(context, cancellationTokenSource.Token);
            }
            else
            {
                SendErrorResponse(context, "Extension '{0}' is not registered.", extensionName);
            }
        }

        private void OnRequestMessage(HttpListenerContext context)
        {
            var clientName = context.Request.QueryString.Get("name");
            var timestampString = context.Request.QueryString.Get("timestamp");
            ulong timestamp;
            if (ulong.TryParse(timestampString, out timestamp))
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (this.latestMessageTimestamp > timestamp)
                    {
                        var message = messageQueue.FirstOrDefault(
                            x => x.Timestamp > timestamp 
                                 && (x.IsBroadcast || x.To == clientName));
                        if (message != null)
                        {
                            SendJsonResponse(context, message.GetJson());
                            return;
                        }
                    }
                    Thread.Sleep(50);
                }
                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    SendErrorResponse(context, "Server stopped.");
                }
                else
                {
                    SendErrorResponse(context, "Connection closed for unknown reason.");
                }
            }
            else
            {
                SendErrorResponse(context, "Could not parse timestamp '{0}'.", timestampString);
            }
        }

        private void OnReceiveMessage(HttpListenerContext context, bool isBroadcast)
        {
            var from = context.Request.QueryString.Get("from");
            if (from == null)
            {
                SendErrorResponse(context, "Missing parameter 'from'.");
                return;
            }
            var to = context.Request.QueryString.Get("to");
            if (!isBroadcast)
            {
                if (to == null)
                {
                    SendErrorResponse(context, "Missing parameter 'to'.");
                    return;
                }
            }
            else
            {
                to = null;
            }
            var message = context.Request.QueryString.Get("message");
            if (message == null)
            {
                SendErrorResponse(context, "Missing parameter 'message'.");
                return;
            }

            var timestamp = (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            var messageEntry = new MessageEntry(from, to, message, isBroadcast, timestamp);
            this.messageQueue.Enqueue(messageEntry);

            if (this.messageQueue.Count > 20)
            {
                this.messageQueue.Dequeue();
            }

            this.latestMessageTimestamp = timestamp;

            SendDefaultResponse(context);
        }

        public static void SendJsonResponse(HttpListenerContext context, string json)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.AppendHeader("Access-Control-Allow-Origin", "*");

            var writer = new StreamWriter(context.Response.OutputStream);
            writer.Write(json);
            writer.Flush();
            context.Response.Close();
        }

        public static void SendDefaultResponse(HttpListenerContext context)
        {
            SendJsonResponse(context, "{}");
        }

        public static void SendErrorResponse(HttpListenerContext context, string format, params object[] args)
        {
            SendErrorResponse(context, string.Format(format, args));
        }

        public static void SendErrorResponse(HttpListenerContext context, string message)
        {
            SendJsonResponse(
                context,
                string.Format("{{ \"isError\": true, \"message\": \"{0}\" }}", Util.CreateJsonSafeString(message)));
        }

        public void Stop()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                Thread.Sleep(1000);
                cancellationTokenSource.Dispose();
                listener.Close();
                this.running = false;
            }
        }
    }
}
