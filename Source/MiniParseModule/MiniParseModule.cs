using Advanced_Combat_Tracker;
using Nancy;
using RainbowMage.ActServer.Nancy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Modules
{
    public class MiniParseModule : NancyModule
    {
        public enum SortType
        {
            None,
            StringAscending,
            StringDescending,
            NumericAscending,
            NumericDescending
        }

        public MiniParseModule()
        {
            this.After += context =>
            {
                if (context.Response != null)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
            };

            Get["/command/miniparse"] = _ =>
            {
                string sortKey = Request.Query.sortKey;
                SortType sortType;
                string sortTypeString = Request.Query.sortType;
                Enum.TryParse(sortTypeString, true, out sortType);

                while (!CheckIsActReady())
                {
                    Thread.Sleep(100);
                }
                if (!CheckIsActReady())
                {
                    return Response.AsJsonErrorMessage("Server stopped.");
                }

                var json = "";
                try
                {
                    json = CreateJsonData(sortKey, sortType);
                }
                catch (Exception e)
                {
                    return Response.AsJsonErrorMessage(e.ToString());
                }

                return Response.AsJson(json);
            };
        }

        private static string updateStringCache = "";
        private static DateTime updateStringCacheLastUpdate;
        private static readonly TimeSpan UpdateStringCacheExpireInterval = new TimeSpan(0, 0, 0, 0, 500); // 500 msec

        internal string CreateJsonData(string sortKey, SortType sortType)
        {
            if (DateTime.Now - updateStringCacheLastUpdate < UpdateStringCacheExpireInterval)
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
                    catch (Exception)
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
                catch (Exception)
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
