using System;
using Advanced_Combat_Tracker;
using RainbowMage.ActServer.Nancy;
using Nancy;

namespace GameLogModule
{
    public class GameLogModule : NancyModule
    {
        public GameLogModule(IBootstrapParams bootParams)
        {
            if (!bootParams.IsWebSocketAvailable())
            {
                return;
            }

            Get["/websocket/gamelog"] = _ =>
            {
                if (ActGlobals.oFormActMain != null)
                {
                    var webSocket = new NancyWebSocket();
                    var handler = new LogLineEventDelegate(async (o, e) =>
                    {
                        try
                        {
                            var log = new GameLog(e.detectedTime, e.detectedType, e.detectedZone, e.inCombat, e.logLine);
                            await webSocket.SendTextAsync(log.GetJson());
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
