using System;
using System.Collections.Generic;
using System.IO;
using IPA.Utilities;
using Newtonsoft.Json;

namespace MoreFunMultiplay;

public static class SessionStore
{
	public class StoreData
	{
		public string LastActiveDate { get; set; } = "";


		public int DailyMatchesPlayed { get; set; }

		public Dictionary<string, int> DailyScores { get; set; } = new Dictionary<string, int>();


		public int TotalMatchesPlayed { get; set; }

		public Dictionary<string, int> TotalScores { get; set; } = new Dictionary<string, int>();

	}

	private static readonly string FilePath = Path.Combine(UnityGame.UserDataPath, "MoreFunMultiplay_Session.json");

	public static StoreData Current { get; private set; } = new StoreData();


	private static string GetGameDateString()
	{
		return DateTime.Now.AddHours(-5.0).ToString("yyyy-MM-dd");
	}

	public static void Load()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				Current = JsonConvert.DeserializeObject<StoreData>(File.ReadAllText(FilePath)) ?? new StoreData();
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
			Plugin.Log.Error("Failed to load session data: " + ex.Message);
			Current = new StoreData();
		}
	}

	public static void CheckDateRollover()
	{
		string gameDateString = GetGameDateString();
		if (string.IsNullOrEmpty(Current.LastActiveDate))
		{
			Current.LastActiveDate = gameDateString;
		}
		else if (Current.LastActiveDate != gameDateString)
		{
			Plugin.Log.Info("Daily rollover (5:00 AM) triggered from " + Current.LastActiveDate + " to " + gameDateString + ". Resetting Daily scores.");
			Current.LastActiveDate = gameDateString;
			Current.DailyScores.Clear();
			Current.DailyMatchesPlayed = 0;
			Save();
		}
	}

	public static void Save()
	{
		try
		{
			string contents = JsonConvert.SerializeObject(Current, Formatting.Indented);
			File.WriteAllText(FilePath, contents);
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("Failed to save session data: " + ex.Message);
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
		if (!string.IsNullOrEmpty(playerName))
		{
			CheckDateRollover();
			if (!Current.DailyScores.ContainsKey(playerName))
			{
				Current.DailyScores[playerName] = 0;
			}
			Current.DailyScores[playerName] += points;
			if (!Current.TotalScores.ContainsKey(playerName))
			{
				Current.TotalScores[playerName] = 0;
			}
			Current.TotalScores[playerName] += points;
			Save();
		}
	}
}
