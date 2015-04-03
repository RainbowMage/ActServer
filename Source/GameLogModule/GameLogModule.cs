using System;
using Advanced_Combat_Tracker;
using RainbowMage.ActServer.Nancy;
using Nancy;
using RainbowMage.ActServer;

namespace GameLogModule
{
    public class GameLogModule : NancyModule
    {
        public GameLogModule(IConfiguration configuration)
        {
            if (!configuration.IsWebSocketAvailable())
            {
                return;
            }

            Get["/websocket/log"] = _ =>
            {
                if (ActGlobals.oFormActMain != null)
                {
                    var webSocket = new NancyWebSocket();
                    var handler = new LogLineEventDelegate(async (o, e) =>
                    {
                        try
                        {
                            var log = string.Format(
                                "{0}:{1}:{2}",
                                e.detectedTime,
                                e.detectedType,
                                e.logLine);
                            await webSocket.SendTextAsync(log);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    });
                    ActGlobals.oFormActMain.OnLogLineRead += handler;
                    webSocket.Disposed += (o, e) =>
                    {
                        ActGlobals.oFormActMain.OnLogLineRead -= handler;
                    };

                    return webSocket.AcceptWebSocketRequest(Context);
                }

                return 404;
            };
        }
    }
}
