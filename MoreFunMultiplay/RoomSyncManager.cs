using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoreFunMultiplay;

public static class RoomSyncManager
{
	private const string RelayBaseUrl = "https://ntfy.sh";

	private const string RoomTopicPrefix = "mfm_bs_room_";

	private const string ActiveRoomsTopic = "mfm_bs_active_rooms";

	private static readonly ConcurrentQueue<RoomRankingSnapshot> _incomingSnapshots = new ConcurrentQueue<RoomRankingSnapshot>();

	private static long _lastAppliedTimestampMs = 0L;

	private static string _lastAppliedRoomCode = "";

	private static long _lastActiveRoomAnnounceMs = 0L;

	private static bool _isPolling = false;

	private static readonly object _publishLock = new object();

	private static CancellationTokenSource _debounceCts = null;

	private static long _rateLimitCooldownUntilMs = 0L;

	public static string SanitizeRoomCode(string rawCode)
	{
		if (string.IsNullOrWhiteSpace(rawCode))
		{
			return "";
		}
		string text = Regex.Replace(rawCode.Trim().ToUpperInvariant(), "[^A-Z0-9_-]", "");
		if (text.Length > 24)
		{
			text = text.Substring(0, 24);
		}
		return text;
	}

	public static void PublishHostSnapshot(RoomRankingSnapshot snapshot, bool isSongEndUpdate)
	{
		if (snapshot == null)
		{
			return;
		}
		string text = SanitizeRoomCode(snapshot.RoomCode);
		if (!string.IsNullOrEmpty(text))
		{
			snapshot.RoomCode = text;
			lock (_publishLock)
			{
				_debounceCts?.Cancel();
				_debounceCts = null;
			}
			ExecutePublish(snapshot, isSongEndUpdate);
		}
	}

	private static void ExecutePublish(RoomRankingSnapshot snapshot, bool isSongEndUpdate)
	{
		long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		if (num < _rateLimitCooldownUntilMs && !isSongEndUpdate)
		{
			return;
		}
		snapshot.TimestampUnixMs = num;
		snapshot.IsSongEndUpdate = isSongEndUpdate;
		Task.Run(delegate
		{
			try
			{
				string text = JsonConvert.SerializeObject(snapshot);
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				string body = text;
				if (bytes.Length > 3000)
				{
					string gz = CompressToBase64(bytes);
					body = JsonConvert.SerializeObject(new
					{
						v = 1,
						ts = snapshot.TimestampUnixMs,
						room = snapshot.RoomCode,
						host = snapshot.HostName,
						gz = gz
					});
				}
				PostString("https://ntfy.sh/mfm_bs_room_" + snapshot.RoomCode, body);
				long num2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				if (isSongEndUpdate || num2 - _lastActiveRoomAnnounceMs > 30000)
				{
					_lastActiveRoomAnnounceMs = num2;
					string modeDisplayName = GetModeDisplayName((GameMode)snapshot.SelectedGameMode, snapshot.TargetScorePercentage);
					string body2 = JsonConvert.SerializeObject(new
					{
						room = snapshot.RoomCode,
						host = (snapshot.HostName ?? "Host"),
						mode = modeDisplayName,
						dailyMatches = snapshot.DailyMatchesPlayed,
						totalMatches = snapshot.TotalMatchesPlayed,
						ts = snapshot.TimestampUnixMs
					});
					PostString("https://ntfy.sh/mfm_bs_active_rooms", body2);
				}
			}
			catch (WebException ex)
			{
				if (ex.Response is HttpWebResponse { StatusCode: HttpStatusCode.TooManyRequests })
				{
					_rateLimitCooldownUntilMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 30000;
					Plugin.Log.Warn("RoomSyncManager hit ntfy rate limit (429 Too Many Requests). Cooldown for 30s.");
				}
				else
				{
					Plugin.Log.Warn("RoomSyncManager PublishHostSnapshot warning: " + ex.Message);
				}
			}
			catch (Exception ex2)
			{
				Plugin.Log.Warn("RoomSyncManager PublishHostSnapshot warning: " + ex2.Message);
			}
		});
	}

	public static void TriggerParticipantFetchWithRetries(string rawRoomCode)
	{
		string roomCode = SanitizeRoomCode(rawRoomCode);
		if (string.IsNullOrEmpty(roomCode))
		{
			return;
		}
		Task.Run(async delegate
		{
			int[] array = new int[2] { 1200, 3000 };
			int[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				await Task.Delay(array2[i]).ConfigureAwait(continueOnCapturedContext: false);
				if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom() || MultiplayerPlusBridge.IsLocalPlayerRoomHost())
				{
					break;
				}
				FetchLatestSnapshotBlocking(roomCode);
			}
		});
	}

	public static void PollLatestRoomSnapshotAsync(string rawRoomCode)
	{
		if (_isPolling)
		{
			return;
		}
		string roomCode = SanitizeRoomCode(rawRoomCode);
		if (string.IsNullOrEmpty(roomCode))
		{
			return;
		}
		_isPolling = true;
		Task.Run(delegate
		{
			try
			{
				FetchLatestSnapshotBlocking(roomCode);
			}
			finally
			{
				_isPolling = false;
			}
		});
	}

	public static void ProcessMainThreadQueue()
	{
		RoomRankingSnapshot result;
		while (_incomingSnapshots.TryDequeue(out result))
		{
			if (result != null)
			{
				ResultsUIHost.Instance.ApplyRemoteSnapshot(result);
			}
		}
	}

	public static void ResetAppliedState()
	{
		_lastAppliedTimestampMs = 0L;
		_lastAppliedRoomCode = "";
	}

	private static void FetchLatestSnapshotBlocking(string roomCode)
	{
		try
		{
			HttpWebRequest obj = (HttpWebRequest)WebRequest.Create("https://ntfy.sh/mfm_bs_room_" + roomCode + "/json?poll=1&since=6h");
			obj.Method = "GET";
			obj.Timeout = 6000;
			obj.ReadWriteTimeout = 6000;
			obj.UserAgent = "MoreFunMultiplay/1.0";
			using HttpWebResponse httpWebResponse = (HttpWebResponse)obj.GetResponse();
			using Stream stream = httpWebResponse.GetResponseStream();
			using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
			string text = null;
			string text2;
			while ((text2 = streamReader.ReadLine()) != null)
			{
				if (string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				try
				{
					JObject jObject = JObject.Parse(text2);
					if ((string?)jObject["event"] == "message")
					{
						string text3 = (string?)jObject["message"];
						if (!string.IsNullOrWhiteSpace(text3) && text3.StartsWith("{"))
						{
							text = text3;
						}
					}
				}
				catch
				{
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				RoomRankingSnapshot roomRankingSnapshot = ParseSnapshotJson(text);
				if (roomRankingSnapshot != null && !string.IsNullOrEmpty(roomRankingSnapshot.RoomCode) && (roomRankingSnapshot.RoomCode != _lastAppliedRoomCode || roomRankingSnapshot.TimestampUnixMs > _lastAppliedTimestampMs))
				{
					_lastAppliedRoomCode = roomRankingSnapshot.RoomCode;
					_lastAppliedTimestampMs = roomRankingSnapshot.TimestampUnixMs;
					_incomingSnapshots.Enqueue(roomRankingSnapshot);
				}
			}
		}
		catch
		{
		}
	}

	private static RoomRankingSnapshot ParseSnapshotJson(string rawJson)
	{
		try
		{
			JObject jObject = JObject.Parse(rawJson);
			if (jObject["gz"] != null)
			{
				return JsonConvert.DeserializeObject<RoomRankingSnapshot>(DecompressFromBase64((string?)jObject["gz"]));
			}
			return jObject.ToObject<RoomRankingSnapshot>();
		}
		catch
		{
			return null;
		}
	}

	private static void PostString(string url, string body)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(body);
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		httpWebRequest.Method = "POST";
		httpWebRequest.ContentType = "text/plain; charset=utf-8";
		httpWebRequest.ContentLength = bytes.Length;
		httpWebRequest.Timeout = 7000;
		httpWebRequest.ReadWriteTimeout = 7000;
		httpWebRequest.UserAgent = "MoreFunMultiplay/1.0";
		using (Stream stream = httpWebRequest.GetRequestStream())
		{
			stream.Write(bytes, 0, bytes.Length);
		}
		using ((HttpWebResponse)httpWebRequest.GetResponse())
		{
		}
	}

	private static string CompressToBase64(byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
		{
			gZipStream.Write(data, 0, data.Length);
		}
		return Convert.ToBase64String(memoryStream.ToArray());
	}

	private static string DecompressFromBase64(string base64)
	{
		using MemoryStream stream = new MemoryStream(Convert.FromBase64String(base64));
		using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
		using MemoryStream memoryStream = new MemoryStream();
		gZipStream.CopyTo(memoryStream);
		return Encoding.UTF8.GetString(memoryStream.ToArray());
	}

	private static string GetModeDisplayName(GameMode mode, float targetPct)
	{
		return mode switch
		{
			GameMode.Sweat => "\ud83d\udd25 運動量ランキング", 
			GameMode.Jizo => "\ud83d\uddff 地蔵スタイル賞", 
			GameMode.ChickenRace => $"\ud83c\udfaf チキンレース ({targetPct:F0}%)", 
			GameMode.Symmetry => "⚖\ufe0f シンメトリー", 
			GameMode.Wild => "\ud83d\udca5 Wild", 
			_ => "勝負なし (通常)", 
		};
	}
}
