using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoreFunMultiplay
{
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
        public string[] TabContents = new string[5];

        [JsonProperty("lobbyTabs")]
        public string[] LobbyTabContents = new string[5];

        [JsonProperty("counts")]
        public int[] TabItemCounts = new int[5];
    }

    public static class RoomSyncManager
    {
        private const string RelayBaseUrl = "https://ntfy.sh";
        private const string RoomTopicPrefix = "mfm_bs_room_";
        private const string ActiveRoomsTopic = "mfm_bs_active_rooms";

        private static readonly ConcurrentQueue<RoomRankingSnapshot> _incomingSnapshots = new ConcurrentQueue<RoomRankingSnapshot>();
        private static long _lastAppliedTimestampMs = 0;
        private static string _lastAppliedRoomCode = "";
        private static long _lastActiveRoomAnnounceMs = 0;
        private static bool _isPolling = false;
        private static readonly object _publishLock = new object();
        private static System.Threading.CancellationTokenSource _debounceCts = null;
        private static long _rateLimitCooldownUntilMs = 0;

        public static string SanitizeRoomCode(string rawCode)
        {
            if (string.IsNullOrWhiteSpace(rawCode)) return "";
            string clean = Regex.Replace(rawCode.Trim().ToUpperInvariant(), @"[^A-Z0-9_-]", "");
            if (clean.Length > 24) clean = clean.Substring(0, 24);
            return clean;
        }

        public static void PublishHostSnapshot(RoomRankingSnapshot snapshot, bool isSongEndUpdate)
        {
            if (snapshot == null) return;
            string roomCode = SanitizeRoomCode(snapshot.RoomCode);
            if (string.IsNullOrEmpty(roomCode)) return;

            snapshot.RoomCode = roomCode;

            // 曲開始時・曲終了時（および手動リセット時）のみ送信する設計のため即時送信
            lock (_publishLock)
            {
                _debounceCts?.Cancel();
                _debounceCts = null;
            }
            ExecutePublish(snapshot, isSongEndUpdate);
        }

        private static void ExecutePublish(RoomRankingSnapshot snapshot, bool isSongEndUpdate)
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // 429レート制限中の場合はクールダウン（曲終了時のみ救済再試行）
            if (nowMs < _rateLimitCooldownUntilMs && !isSongEndUpdate)
            {
                return;
            }

            snapshot.TimestampUnixMs = nowMs;
            snapshot.IsSongEndUpdate = isSongEndUpdate;

            Task.Run(() =>
            {
                try
                {
                    string json = JsonConvert.SerializeObject(snapshot);
                    byte[] rawBytes = Encoding.UTF8.GetBytes(json);

                    string payloadToSend = json;
                    if (rawBytes.Length > 3000)
                    {
                        string gzBase64 = CompressToBase64(rawBytes);
                        payloadToSend = JsonConvert.SerializeObject(new
                        {
                            v = 1,
                            ts = snapshot.TimestampUnixMs,
                            room = snapshot.RoomCode,
                            host = snapshot.HostName,
                            gz = gzBase64
                        });
                    }

                    string roomUrl = $"{RelayBaseUrl}/{RoomTopicPrefix}{snapshot.RoomCode}";
                    PostString(roomUrl, payloadToSend);

                    long currentMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    // 稼働中部屋アナウンスは曲終了時、または30秒以上の間隔でのみ送信
                    if (isSongEndUpdate || (currentMs - _lastActiveRoomAnnounceMs) > 30000)
                    {
                        _lastActiveRoomAnnounceMs = currentMs;
                        string modeName = GetModeDisplayName((GameMode)snapshot.SelectedGameMode, snapshot.TargetScorePercentage);
                        string announceJson = JsonConvert.SerializeObject(new
                        {
                            room = snapshot.RoomCode,
                            host = snapshot.HostName ?? "Host",
                            mode = modeName,
                            dailyMatches = snapshot.DailyMatchesPlayed,
                            totalMatches = snapshot.TotalMatchesPlayed,
                            ts = snapshot.TimestampUnixMs
                        });
                        PostString($"{RelayBaseUrl}/{ActiveRoomsTopic}", announceJson);
                    }
                }
                catch (WebException webEx)
                {
                    if (webEx.Response is HttpWebResponse httpResp && (int)httpResp.StatusCode == 429)
                    {
                        _rateLimitCooldownUntilMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 30000;
                        Plugin.Log.Warn("RoomSyncManager hit ntfy rate limit (429 Too Many Requests). Cooldown for 30s.");
                        return;
                    }
                    Plugin.Log.Warn("RoomSyncManager PublishHostSnapshot warning: " + webEx.Message);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warn("RoomSyncManager PublishHostSnapshot warning: " + ex.Message);
                }
            });
        }

        public static void TriggerParticipantFetchWithRetries(string rawRoomCode)
        {
            string roomCode = SanitizeRoomCode(rawRoomCode);
            if (string.IsNullOrEmpty(roomCode)) return;

            Task.Run(async () =>
            {
                int[] delaysMs = new int[] { 1200, 3000 };
                foreach (int delay in delaysMs)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                    if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom() || MultiplayerPlusBridge.IsLocalPlayerRoomHost())
                        return;
                    FetchLatestSnapshotBlocking(roomCode);
                }
            });
        }

        public static void PollLatestRoomSnapshotAsync(string rawRoomCode)
        {
            if (_isPolling) return;
            string roomCode = SanitizeRoomCode(rawRoomCode);
            if (string.IsNullOrEmpty(roomCode)) return;

            _isPolling = true;
            Task.Run(() =>
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
            while (_incomingSnapshots.TryDequeue(out var snapshot))
            {
                if (snapshot != null)
                {
                    ResultsUIHost.Instance.ApplyRemoteSnapshot(snapshot);
                }
            }
        }

        public static void ResetAppliedState()
        {
            _lastAppliedTimestampMs = 0;
            _lastAppliedRoomCode = "";
        }

        private static void FetchLatestSnapshotBlocking(string roomCode)
        {
            try
            {
                string url = $"{RelayBaseUrl}/{RoomTopicPrefix}{roomCode}/json?poll=1&since=6h";
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 6000;
                req.ReadWriteTimeout = 6000;
                req.UserAgent = "MoreFunMultiplay/1.0";

                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var stream = resp.GetResponseStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string lastMessageBody = null;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            var env = JObject.Parse(line);
                            if ((string)env["event"] == "message")
                            {
                                string msg = (string)env["message"];
                                if (!string.IsNullOrWhiteSpace(msg) && msg.StartsWith("{"))
                                {
                                    lastMessageBody = msg;
                                }
                            }
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(lastMessageBody))
                    {
                        var snapshot = ParseSnapshotJson(lastMessageBody);
                        if (snapshot != null && !string.IsNullOrEmpty(snapshot.RoomCode))
                        {
                            if (snapshot.RoomCode != _lastAppliedRoomCode || snapshot.TimestampUnixMs > _lastAppliedTimestampMs)
                            {
                                _lastAppliedRoomCode = snapshot.RoomCode;
                                _lastAppliedTimestampMs = snapshot.TimestampUnixMs;
                                _incomingSnapshots.Enqueue(snapshot);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore transient network timeouts
            }
        }

        private static RoomRankingSnapshot ParseSnapshotJson(string rawJson)
        {
            try
            {
                var obj = JObject.Parse(rawJson);
                if (obj["gz"] != null)
                {
                    string gzBase64 = (string)obj["gz"];
                    string decompressedJson = DecompressFromBase64(gzBase64);
                    return JsonConvert.DeserializeObject<RoomRankingSnapshot>(decompressedJson);
                }
                return obj.ToObject<RoomRankingSnapshot>();
            }
            catch
            {
                return null;
            }
        }

        private static void PostString(string url, string body)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "text/plain; charset=utf-8";
            req.ContentLength = bytes.Length;
            req.Timeout = 7000;
            req.ReadWriteTimeout = 7000;
            req.UserAgent = "MoreFunMultiplay/1.0";

            using (var reqStream = req.GetRequestStream())
            {
                reqStream.Write(bytes, 0, bytes.Length);
            }
            using (var resp = (HttpWebResponse)req.GetResponse())
            {
                // Completed
            }
        }

        private static string CompressToBase64(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    gz.Write(data, 0, data.Length);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private static string DecompressFromBase64(string base64)
        {
            byte[] compressed = Convert.FromBase64String(base64);
            using (var inMs = new MemoryStream(compressed))
            using (var gz = new GZipStream(inMs, CompressionMode.Decompress))
            using (var outMs = new MemoryStream())
            {
                gz.CopyTo(outMs);
                return Encoding.UTF8.GetString(outMs.ToArray());
            }
        }

        private static string GetModeDisplayName(GameMode mode, float targetPct)
        {
            switch (mode)
            {
                case GameMode.Sweat: return "🔥 運動量ランキング";
                case GameMode.Jizo: return "🗿 地蔵スタイル賞";
                case GameMode.ChickenRace: return $"🎯 チキンレース ({targetPct:F0}%)";
                default: return "勝負なし (通常)";
            }
        }
    }
}
