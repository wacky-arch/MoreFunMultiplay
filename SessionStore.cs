using System;
using System.Collections.Generic;
using System.IO;
using IPA.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace MoreFunMultiplay
{
    public enum GameMode
    {
        None = 0,           // 勝負なし（通常）
        Sweat = 1,          // 運動量ランキング
        Jizo = 2,           // 地蔵スタイル賞
        ChickenRace = 3     // ターゲットスコアチキンレース
    }

    public static class SessionStore
    {
        private static readonly string FilePath = Path.Combine(UnityGame.UserDataPath, "MoreFunMultiplay_Session.json");

        public class StoreData
        {
            // 朝5時基準の日付キー (yyyy-MM-dd)
            public string LastActiveDate { get; set; } = "";

            // 当日（Daily）記録
            public int DailyMatchesPlayed { get; set; } = 0;
            public Dictionary<string, int> DailyScores { get; set; } = new Dictionary<string, int>();

            // 通算（Total）記録
            public int TotalMatchesPlayed { get; set; } = 0;
            public Dictionary<string, int> TotalScores { get; set; } = new Dictionary<string, int>();
        }

        public static StoreData Current { get; private set; } = new StoreData();

        /// <summary>
        /// 朝5時を日付の区切り（境界線）とするゲーム上の日付文字列を取得します。
        /// （例: 9月2日 26:00(=9月3日 02:00) は 9月2日扱い。9月3日 05:00 に切り替わり）
        /// </summary>
        private static string GetGameDateString()
        {
            return DateTime.Now.AddHours(-5).ToString("yyyy-MM-dd");
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    Current = JsonConvert.DeserializeObject<StoreData>(json) ?? new StoreData();
                    Plugin.Log.Info($"Loaded session data. Total: {Current.TotalScores.Count} players, Daily: {Current.DailyScores.Count} players.");
                }
                else
                {
                    Current = new StoreData();
                }

                CheckDateRollover();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to load session data: {ex.Message}");
                Current = new StoreData();
            }
        }

        public static void CheckDateRollover()
        {
            string gameDate = GetGameDateString();
            if (string.IsNullOrEmpty(Current.LastActiveDate))
            {
                Current.LastActiveDate = gameDate;
            }
            else if (Current.LastActiveDate != gameDate)
            {
                Plugin.Log.Info($"Daily rollover (5:00 AM) triggered from {Current.LastActiveDate} to {gameDate}. Resetting Daily scores.");
                Current.LastActiveDate = gameDate;
                Current.DailyScores.Clear();
                Current.DailyMatchesPlayed = 0;
                Save();
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Current, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to save session data: {ex.Message}");
            }
        }

        public static void ResetDaily()
        {
            Current.DailyScores.Clear();
            Current.DailyMatchesPlayed = 0;
            Current.LastActiveDate = GetGameDateString();
            Save();
            Plugin.Log.Info("Daily scores reset.");
        }

        public static void ResetTotal()
        {
            Current.TotalScores.Clear();
            Current.TotalMatchesPlayed = 0;
            Save();
            Plugin.Log.Info("Total scores reset.");
        }

        public static void AddPoints(string playerName, int points)
        {
            if (string.IsNullOrEmpty(playerName)) return;

            CheckDateRollover();

            // 当日（Daily）
            if (!Current.DailyScores.ContainsKey(playerName))
                Current.DailyScores[playerName] = 0;
            Current.DailyScores[playerName] += points;

            // 通算（Total）
            if (!Current.TotalScores.ContainsKey(playerName))
                Current.TotalScores[playerName] = 0;
            Current.TotalScores[playerName] += points;

            Save();
        }
    }
}
