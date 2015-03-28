using Advanced_Combat_Tracker;
using RainbowMage.ActServer.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Extensions
{
    public class MiniParseExtension : IExtension
    {
        #region IExtension
        public string ExtensionName
        {
            get { return "RainbowMage.MiniParse"; }
        }

        public string DisplayName
        {
            get { return "Mini parse"; }
        }

        public string Description
        {
            get { return "Provides JSON data about combat."; }
        }

        public enum SortType
        {
            None,
            StringAscending,
            StringDescending,
            NumericAscending,
            NumericDescending
        }

        public void ProcessRequest(HttpListenerContext context, CancellationToken token)
        {
            var sortKey = context.Request.QueryString.Get("sortKey");
            SortType sortType;
            var sortTypeString = context.Request.QueryString.Get("sortType");
            Enum.TryParse(sortTypeString, true, out sortType);

            while (!token.IsCancellationRequested && !CheckIsActReady())
            {
                Thread.Sleep(100);
            }
            if (!CheckIsActReady())
            {
                Server.SendErrorResponse(context, "Server stopped.");
            }

            var json = "";
            try
            {
                json = CreateJsonData(sortKey, sortType, token);
            }
            catch (Exception e)
            {
                Server.SendErrorResponse(context, e.ToString());
                return;
            }

            Server.SendJsonResponse(context, json);
        }

        public void Dispose()
        {

        }
        #endregion

        private static string updateStringCache = "";
        private static DateTime updateStringCacheLastUpdate;
        private static readonly TimeSpan updateStringCacheExpireInterval = new TimeSpan(0, 0, 0, 0, 500); // 500 msec

        internal string CreateJsonData(string sortKey, SortType sortType, CancellationToken token)
        {
            if (DateTime.Now - updateStringCacheLastUpdate < updateStringCacheExpireInterval)
            {
                return updateStringCache;
            }

#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var allies = ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.GetAllies();
            Dictionary<string, string> encounter = null;
            OrderedDictionary<string, Dictionary<string, string>> combatant = null;

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

            var response = new MiniParseResponse();
            response.Encounter = encounter;
            response.Combatant = combatant;
            response.IsActive = ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.Active;

#if DEBUG
            stopwatch.Stop();
            response.ProcessingTime = stopwatch.Elapsed.TotalMilliseconds;
#endif

            var result = response.GetJson();

            updateStringCache = result;
            updateStringCacheLastUpdate = DateTime.Now;

            return result;
        }

        private void SortCombatantList(
            OrderedDictionary<string, Dictionary<string, string>> combatant, 
            string sortKey,
            SortType sortType)
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

        private OrderedDictionary<string, Dictionary<string, string>> GetCombatantList(List<CombatantData> allies)
        {
            var combatantList = new OrderedDictionary<string, Dictionary<string, string>>();
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
                    combatantList.Add(ally.Name, valueDict);
                }
            }
            );

            return combatantList;
        }

        private Dictionary<string, string> GetEncounterDictionary(List<CombatantData> allies)
        {
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
