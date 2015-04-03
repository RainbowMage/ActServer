using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.Owin;

namespace RainbowMage.ActServer.Nancy
{
    using WebSocketAccept = Action<
              IDictionary<string, object>,     // WebSocket Accept parameters
              Func<                            // WebSocketFunc callback
                  IDictionary<string, object>, // WebSocket environment
                  Task>>;

    using WebSocketSendAsync = Func<
                   ArraySegment<byte>,      // data
                   int,                     // message type
                   bool,                    // end of message
                   CancellationToken,       // cancel
                   Task>;

    using WebSocketReceiveAsync = Func<
                ArraySegment<byte>,         // data
                CancellationToken,          // cancel
                Task<
                    Tuple<                  // WebSocketReceiveTuple
                        int,                // messageType
                        bool,               // endOfMessage
                        int>>>;             // count

    using WebSocketCloseAsync = Func<
                int,                        // closeStatus
                string,                     // closeDescription
                CancellationToken,          // cancel
                Task>;

    public class NancyWebSocket : IDisposable
    {
        private IDictionary<string, object> webSocketEnv;
        private CancellationTokenSource connectionCts;
        private CancellationToken connectionToken;

        public event EventHandler<MessageReceivedEventArgs> Received;
        public event EventHandler Disconnected;
        public event EventHandler Disposed;

        public bool IsDisposed { get; private set; }

        #region Wrapper

        public string Version { get { return (string)webSocketEnv["websocket.Version"]; } }
        public CancellationToken ClientToken { get { return (CancellationToken)webSocketEnv["websocket.CallCancelled"]; } }

        public Task SendAsync(ArraySegment<byte> data, WebSocketMessageType messageType, bool isEndOfMessage, CancellationToken token)
        {
            var sendAsync = (WebSocketSendAsync)webSocketEnv["websocket.SendAsync"]; ;
            return sendAsync(data, (int)messageType, isEndOfMessage, token);
        }

        private async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> data, CancellationToken token)
        {
            var receiveAsync = (WebSocketReceiveAsync)webSocketEnv["websocket.ReceiveAsync"]; ;
            var result = await receiveAsync(data, token);
            return new WebSocketReceiveResult(result.Item1, result.Item2, result.Item3);
        }

        public Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken token)
        {
            var closeAsync = (WebSocketCloseAsync)webSocketEnv["websocket.CloseAsync"];
            return closeAsync((int)status, description, token);

        }

        #endregion

        public NancyWebSocket()
        {
            this.connectionCts = new CancellationTokenSource();
            this.connectionToken = this.connectionCts.Token;
        }

        #region Request acception and message receiving

        /// <summary>
        /// Accept the request and start receiving data from client.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>HTTP status code</returns>
        public int AcceptWebSocketRequest(NancyContext context)
        {
            var env = GetEnvironmentDictonary(context);
            if (env == null)
            {
                return 404;
            }

            // check if the owin host supports web sockets
            var accept = GetWebSocketAcceptDelegate(env);
            if (accept == null)
            {
                return 404;
            }

            var acceptOptions = GetAcceptOptions(env);

            accept(acceptOptions, async wsEnv =>
            {
                this.webSocketEnv = wsEnv;
                await this.AcceptHandler();
            });

            return 200;
        }

        private WebSocketAccept GetWebSocketAcceptDelegate(IDictionary<string, object> env)
        {
            object accept;
            if (env.TryGetValue("websocket.Accept", out accept))
            {
                return accept as WebSocketAccept;
            }

            return null;
        }

        private static IDictionary<string, object> GetEnvironmentDictonary(NancyContext context)
        {
            if (context.Items.ContainsKey(NancyMiddleware.RequestEnvironmentKey))
            {
                return context.Items[NancyMiddleware.RequestEnvironmentKey] as IDictionary<string, object>;
            }

            return null;
        }

        private static Dictionary<string, object> GetAcceptOptions(IDictionary<string, object> env)
        {
            Dictionary<string, object> acceptOptions = null;

            var requestHeaders = GetValue<IDictionary<string, string[]>>(env, "owin.RequestHeaders");
            string[] subProtocols;
            if (requestHeaders.TryGetValue("Sec-WebSocket-Protocol", out subProtocols)
                && subProtocols.Length > 0)
            {
                acceptOptions = new Dictionary<string, object>();
                // Select the first one from the client
                acceptOptions.Add("websocket.SubProtocol", subProtocols[0].Split(',').First().Trim());
            }

            return acceptOptions;
        }

        private static T GetValue<T>(IDictionary<string, object> env, string key)
        {
            object value;
            return env.TryGetValue(key, out value) && value is T
                ? (T)value
                : default(T);
        }

        private static string GetHeader(IDictionary<string, string[]> headers, string key)
        {
            string[] value;
            return headers.TryGetValue(key, out value) && value != null
                ? string.Join(",", value.ToArray())
                : null;
        }

        public async Task AcceptHandler()
        {
            while (!this.connectionToken.IsCancellationRequested)
            {
                await ReceiveHandler();
            }

            // Connection has cancelled
            await this.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closing",
                CancellationToken.None);

            // Fire Disconnect event
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
            }

            this.Dispose();
        }

        private async Task ReceiveHandler()
        {
            using (var memoryStream = new MemoryStream())
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                while (!this.connectionToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await this.ReceiveAsync(buffer, this.connectionToken);

                        memoryStream.Write(buffer.Array, buffer.Offset, result.Count);

                        if (result.IsEndOfMessage)
                            break;
                    }
                    catch (WebSocketException exception)
                    {
                        // connection refused
                        if (exception.HResult == -2147467259)
                        {
                            this.connectionCts.Cancel();
                        }
                    }
                }

                if (this.connectionToken.IsCancellationRequested)
                {
                    return;
                }

                if (Received != null)
                {
                    byte[] message = memoryStream.ToArray();

                    try
                    {
                        Received(this, new MessageReceivedEventArgs(message));
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Send binary data to the client asynchronously.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task SendBinaryAsync(byte[] data, CancellationToken token = default(CancellationToken))
        {
            try
            {
                if (!this.connectionToken.IsCancellationRequested)
                {
                    await this.SendAsync(
                        new ArraySegment<byte>(data),
                        WebSocketMessageType.Binary,
                        true,
                        token);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Send text data to the client asynchronously.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task SendTextAsync(string text, CancellationToken token = default(CancellationToken))
        {
            try
            {
                if (!this.connectionToken.IsCancellationRequested)
                {
                    await this.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)),
                        WebSocketMessageType.Binary,
                        true,
                        token);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Disconnect()
        {
            this.connectionCts.Cancel();
        }

        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                this.Disconnect();

                if (Disposed != null)
                {
                    Disposed(this, EventArgs.Empty);
                }

                this.IsDisposed = true;
            }
        }

        private class WebSocketReceiveResult
        {
            public int MessageType { get; private set; }
            public bool IsEndOfMessage { get; private set; }
            public int Count { get; private set; }

            public WebSocketReceiveResult(int messageType, bool isEndOfMessage, int count)
            {
                this.MessageType = messageType;
                this.IsEndOfMessage = isEndOfMessage;
                this.Count = count;
            }
        }
    }
}
