using System;
using Advanced_Combat_Tracker;
using RainbowMage.ActServer.Nancy;
using Nancy;

namespace GameLogModule
{
    public class GameLogModule : NancyModule
    {
        public GameLogModule(IBootstrapParams bootParams, ILog log)
        {
            log.Info("GameLogModule loaded.");

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
                            var gamelog = new GameLog(e.detectedTime, e.detectedType, e.detectedZone, e.inCombat, e.logLine);
                            await webSocket.SendTextAsync(gamelog.GetJson());
                        }
                        catch (Exception ex)
                        {
                            log.Error("GameLogModule: {0}", ex);
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
