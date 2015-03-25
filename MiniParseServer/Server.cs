using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RainbowMage.MiniParseServer
{
    class Server
    {
        private HttpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private bool running = false;
        private int port;

        private static string updateStringCache = "";
        private static DateTime updateStringCacheLastUpdate;
        private static readonly TimeSpan updateStringCacheExpireInterval = new TimeSpan(0, 0, 0, 0, 500); // 500 msec

        public enum SortType
        {
            None,
            StringAscending,
            StringDescending,
            NumericAscending,
            NumericDescending
        }

        public Server(int port)
        {
            this.port = port;
        }

        public void Start()
        {
            if (this.running)
            {
                return;
            }

            this.running = true;

            this.listener = new HttpListener();
            this.listener.Prefixes.Add(string.Format("http://*:{0}/", port));

            this.cancellationTokenSource = new CancellationTokenSource();
            this.listener.Start();

            var token = cancellationTokenSource.Token;
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    var result = this.listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
                    result.AsyncWaitHandle.WaitOne(500);
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
                    var sortKey = context.Request.QueryString.Get("sortKey");
                    SortType sortType;
                    var sortTypeString = context.Request.QueryString.Get("sortType");
                    Enum.TryParse(sortTypeString, true, out sortType);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "application/json";
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.AppendHeader("Access-Control-Allow-Origin", "*");

                    var writer = new StreamWriter(context.Response.OutputStream);
                    writer.Write(CreateJsonData(sortKey, sortType));
                    writer.Flush();
                    context.Response.Close();
                }
            }
            catch
            {
                return;
            }
        }

        public void Stop()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Token.WaitHandle.WaitOne(1000);
                cancellationTokenSource.Dispose();
                listener.Close();
                this.running = false;
            }
        }

        internal string CreateJsonData(string sortKey, SortType sortType)
        {
            if (DateTime.Now - updateStringCacheLastUpdate < updateStringCacheExpireInterval)
            {
                return updateStringCache;
            }

            while (!cancellationTokenSource.Token.IsCancellationRequested && !CheckIsActReady())
            {
                Thread.Sleep(100);
            }

            if (!CheckIsActReady())
            {
                return "{}";
            }

#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var allies = ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.GetAllies();
            Dictionary<string, string> encounter = null;
            List<KeyValuePair<CombatantData, Dictionary<string, string>>> combatant = null;

            var encounterTask = Task.Run(() =>
            {
                encounter = GetEncounterDictionary(allies);
            });
            var combatantTask = Task.Run(() =>
            {
                combatant = GetCombatantList(allies);
                SortCombatantList(combatant, sortKey, sortType);
            });
            Task.WaitAll(encounterTask, combatantTask);

            var builder = new StringBuilder();
            builder.Append("{");
            builder.Append("\"Encounter\": {");
            var isFirst1 = true;
            foreach (var pair in encounter)
            {
                if (isFirst1)
                {
                    isFirst1 = false;
                }
                else
                {
                    builder.Append(",");
                }
                var valueString = Util.CreateJsonSafeString(Util.ReplaceNaNString(pair.Value, "---"));
                builder.AppendFormat("\"{0}\":\"{1}\"", Util.CreateJsonSafeString(pair.Key), valueString);
            }
            builder.Append("},");
            builder.Append("\"Combatant\": {");
            var isFirst2 = true;
            foreach (var pair in combatant)
            {
                if (isFirst2)
                {
                    isFirst2 = false;
                }
                else
                {
                    builder.Append(",");
                }
                builder.AppendFormat("\"{0}\": {{", Util.CreateJsonSafeString(pair.Key.Name));
                var isFirst3 = true;
                foreach (var pair2 in pair.Value)
                {
                    if (isFirst3)
                    {
                        isFirst3 = false;
                    }
                    else
                    {
                        builder.Append(",");
                    }
                    var valueString = Util.CreateJsonSafeString(Util.ReplaceNaNString(pair2.Value, "---"));
                    builder.AppendFormat("\"{0}\":\"{1}\"", Util.CreateJsonSafeString(pair2.Key), valueString);
                }
                builder.Append("}");
            }
            builder.Append("},");
            builder.AppendFormat("\"isActive\": {0}", ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.Active ? "true" : "false");
            builder.Append("}");

#if DEBUG
            stopwatch.Stop();
            //Log(LogLevel.Trace, "CreateUpdateScript: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
#endif

            var result = builder.ToString();

            updateStringCache = result;
            updateStringCacheLastUpdate = DateTime.Now;

            return result;
        }

        private void SortCombatantList(List<KeyValuePair<CombatantData, Dictionary<string, string>>> combatant, string sortKey, SortType sortType)
        {
            // 数値で並び替え
            if (sortType == SortType.NumericAscending ||
                sortType == SortType.NumericDescending)
            {
                combatant.Sort((x, y) =>
                {
                    int result = 0;
                    if (x.Value.ContainsKey(sortKey) &&
                        y.Value.ContainsKey(sortKey))
                    {
                        double xValue, yValue;
                        double.TryParse(x.Value[sortKey].Replace("%", ""), out xValue);
                        double.TryParse(y.Value[sortKey].Replace("%", ""), out yValue);

                        result = xValue.CompareTo(yValue);

                        if (sortType == SortType.NumericDescending)
                        {
                            result *= -1;
                        }
                    }

                    return result;
                });
            }
            // 文字列で並び替え
            else if (
                sortType == SortType.StringAscending ||
                sortType == SortType.StringDescending)
            {
                combatant.Sort((x, y) =>
                {
                    int result = 0;
                    if (x.Value.ContainsKey(sortKey) &&
                        y.Value.ContainsKey(sortKey))
                    {
                        result = x.Value[sortKey].CompareTo(y.Value[sortKey]);

                        if (sortType == SortType.StringDescending)
                        {
                            result *= -1;
                        }
                    }

                    return result;
                });
            }
        }

        private List<KeyValuePair<CombatantData, Dictionary<string, string>>> GetCombatantList(List<CombatantData> allies)
        {
#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var combatantList = new List<KeyValuePair<CombatantData, Dictionary<string, string>>>();
            Parallel.ForEach(allies, (ally) =>
            //foreach (var ally in allies)
            {
                var valueDict = new Dictionary<string, string>();
                foreach (var exportValuePair in CombatantData.ExportVariables)
                {
                    try
                    {
                        // NAME タグには {NAME:8} のようにコロンで区切られたエクストラ情報が必要で、
                        // プラグインの仕組み的に対応することができないので除外する
                        if (exportValuePair.Key == "NAME")
                        {
                            continue;
                        }

                        // ACT_FFXIV_Plugin が提供する LastXXDPS は、
                        // ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items に All キーが存在しない場合に、
                        // プラグイン内で例外が発生してしまい、パフォーマンスが悪化するので代わりに空の文字列を挿入する
                        if (exportValuePair.Key == "Last10DPS" ||
                            exportValuePair.Key == "Last30DPS" ||
                            exportValuePair.Key == "Last60DPS")
                        {
                            if (!ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items.ContainsKey("All"))
                            {
                                valueDict.Add(exportValuePair.Key, "");
                                continue;
                            }
                        }

                        var value = exportValuePair.Value.GetExportString(ally, "");
                        valueDict.Add(exportValuePair.Key, value);
                    }
                    catch (Exception e)
                    {
                        //Log(LogLevel.Error, "GetCombatantList: {0}: {1}: {2}", ally.Name, exportValuePair.Key, e);
                        continue;
                    }
                }

                lock (combatantList)
                {
                    combatantList.Add(new KeyValuePair<CombatantData, Dictionary<string, string>>(ally, valueDict));
                }
            }
            );

#if DEBUG
            stopwatch.Stop();
            //Log(LogLevel.Trace, "GetCombatantList: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
#endif

            return combatantList;
        }

        private Dictionary<string, string> GetEncounterDictionary(List<CombatantData> allies)
        {
#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var encounterDict = new Dictionary<string, string>();
            //Parallel.ForEach(EncounterData.ExportVariables, (exportValuePair) =>
            foreach (var exportValuePair in EncounterData.ExportVariables)
            {
                try
                {
                    // ACT_FFXIV_Plugin が提供する LastXXDPS は、
                    // ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items に All キーが存在しない場合に、
                    // プラグイン内で例外が発生してしまい、パフォーマンスが悪化するので代わりに空の文字列を挿入する
                    if (exportValuePair.Key == "Last10DPS" ||
                        exportValuePair.Key == "Last30DPS" ||
                        exportValuePair.Key == "Last60DPS")
                    {
                        if (!allies.All((ally) => ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items.ContainsKey("All")))
                        {
                            encounterDict.Add(exportValuePair.Key, "");
                            continue;
                        }
                    }

                    var value = exportValuePair.Value.GetExportString(
                        ActGlobals.oFormActMain.ActiveZone.ActiveEncounter,
                        allies,
                        "");
                    //lock (encounterDict)
                    //{
                    encounterDict.Add(exportValuePair.Key, value);
                    //}
                }
                catch (Exception e)
                {
                    //Log(LogLevel.Error, "GetEncounterDictionary: {0}: {1}", exportValuePair.Key, e);
                }
            }
            //);

#if DEBUG
            stopwatch.Stop();
            //Log(LogLevel.Trace, "GetEncounterDictionary: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
#endif

            return encounterDict;
        }

        private static bool CheckIsActReady()
        {
            if (ActGlobals.oFormActMain != null &&
                ActGlobals.oFormActMain.ActiveZone != null &&
                ActGlobals.oFormActMain.ActiveZone.ActiveEncounter != null &&
                EncounterData.ExportVariables != null &&
                CombatantData.ExportVariables != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
