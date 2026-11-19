using Newtonsoft.Json;

namespace MoreFunMultiplay;

public class RoomRankingSnapshot
{
	[JsonProperty("v")]
	public int Version = 1;

	[JsonProperty("ts")]
	public long TimestampUnixMs;

	[JsonProperty("room")]
	public string RoomCode;

	[JsonProperty("host")]
	public string HostName;

	[JsonProperty("mode")]
	public int SelectedGameMode;

	[JsonProperty("target")]
	public float TargetScorePercentage;

	[JsonProperty("tab")]
	public int RecommendedTab;

	[JsonProperty("dailyMatches")]
	public int DailyMatchesPlayed;

	[JsonProperty("totalMatches")]
	public int TotalMatchesPlayed;

	[JsonProperty("songEnded")]
	public bool IsSongEndUpdate;

	[JsonProperty("tabs")]
	public string[] TabContents = new string[7];

	[JsonProperty("lobbyTabs")]
	public string[] LobbyTabContents = new string[7];

	[JsonProperty("counts")]
	public int[] TabItemCounts = new int[7];
}
