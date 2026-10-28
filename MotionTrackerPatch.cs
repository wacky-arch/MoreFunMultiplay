using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MoreFunMultiplay
{
    /// <summary>
    /// ローカルプレイヤー（自分）の頭・両手の移動距離を追跡（Multiplayer+ ルーム内でのみ動作）
    /// </summary>
    [HarmonyPatch(typeof(PlayerTransforms))]
    public class LocalPlayerTrackerPatch
    {
        private static FieldInfo _headField;
        private static FieldInfo _leftField;
        private static FieldInfo _rightField;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(PlayerTransforms __instance)
        {
            try
            {
                if (MultiplayManager.Instance == null || !MultiplayManager.Instance.IsInSong) return;
                if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return;

                if (_headField == null)
                {
                    var t = typeof(PlayerTransforms);
                    _headField = AccessTools.Field(t, "_headTransform");
                    _leftField = AccessTools.Field(t, "_leftHandTransform");
                    _rightField = AccessTools.Field(t, "_rightHandTransform");
                }

                Transform head = _headField?.GetValue(__instance) as Transform;
                Transform left = _leftField?.GetValue(__instance) as Transform;
                Transform right = _rightField?.GetValue(__instance) as Transform;

                if (head == null || left == null || right == null) return;

                string localName = MultiplayerPlusBridge.GetLocalPlayerName();
                MultiplayManager.Instance.LocalPlayerName = localName;
                MultiplayManager.Instance.RegisterPlayer(localName, head, left, right);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in LocalPlayerTrackerPatch.UpdatePostfix: " + ex);
            }
        }
    }

    /// <summary>
    /// Multiplayer+ の曲開始時にのみトラッキングを開始し、プレイ中はリザルト画面を非表示にする
    /// </summary>
    [HarmonyPatch]
    public class MultiplayerPlusLevelStartedPatch
    {
        [HarmonyPrepare]
        static bool Prepare() => TargetMethod() != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Managers.GameplayManager");
            if (type == null) return null;
            return AccessTools.Method(type, "Logic_OnLevelStarted");
        }

        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return;
                ResultsUIHost.Instance.HideFloatingScreen();
                if (MultiplayManager.Instance != null)
                {
                    MultiplayManager.Instance.StartSong();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in MultiplayerPlusLevelStartedPatch: " + ex);
            }
        }
    }

    /// <summary>
    /// ローカルプレイヤーの現在のスコア精度(%)をリアルタイム取得
    /// </summary>
    [HarmonyPatch(typeof(RelativeScoreAndImmediateRankCounter), "UpdateRelativeScoreAndImmediateRank")]
    public class RelativeScoreTrackerPatch
    {
        [HarmonyPostfix]
        static void Postfix(RelativeScoreAndImmediateRankCounter __instance)
        {
            if (MultiplayManager.Instance != null && MultiplayManager.Instance.IsInSong && MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
            {
                MultiplayManager.Instance.LocalPlayerAccuracyPercent = __instance.relativeScore * 100f;
            }
        }
    }

    /// <summary>
    /// Multiplayer+ (BeatSaberPlus_Multiplayer) のリモートプレイヤーアバターの頭・両手を追跡
    /// </summary>
    [HarmonyPatch]
    public class MultiplayerPlusAvatarTrackerPatch
    {
        private static FieldInfo _headField;
        private static FieldInfo _leftField;
        private static FieldInfo _rightField;
        private static PropertyInfo _luidProp;

        [HarmonyPrepare]
        static bool Prepare()
        {
            return CalculateMethod() != null;
        }

        [HarmonyTargetMethod]
        static MethodBase CalculateMethod()
        {
            var type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Components.RemotePlayerAvatarController");
            if (type == null) return null;
            return AccessTools.Method(type, "LateUpdate");
        }

        [HarmonyPostfix]
        static void Postfix(Component __instance)
        {
            try
            {
                if (MultiplayManager.Instance == null || !MultiplayManager.Instance.IsInSong) return;
                if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return;

                if (_headField == null)
                {
                    var t = __instance.GetType();
                    _headField = AccessTools.Field(t, "m_HeadTransform");
                    _leftField = AccessTools.Field(t, "m_LeftHandTransform");
                    _rightField = AccessTools.Field(t, "m_RightHandTransform");
                    _luidProp = AccessTools.Property(t, "PlayerLUID");
                }

                Transform head = _headField?.GetValue(__instance) as Transform;
                Transform left = _leftField?.GetValue(__instance) as Transform;
                Transform right = _rightField?.GetValue(__instance) as Transform;

                if (head == null || left == null || right == null) return;

                uint luid = 0;
                if (_luidProp != null && _luidProp.GetValue(__instance) is uint u)
                {
                    luid = u;
                }
                if (luid == 0) return;

                string playerName = MultiplayerPlusBridge.GetPlayerNameByLUID(luid);
                if (string.IsNullOrEmpty(playerName))
                {
                    // ネットワークスレッド更新中などで名前がまだ取得できないフレームはスキップ（次回フレームで取得）
                    return;
                }

                MultiplayManager.Instance.RegisterPlayer(playerName, head, left, right);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in MultiplayerPlusAvatarTrackerPatch: " + ex);
            }
        }
    }

    /// <summary>
    /// BeatSaberPlus_Multiplayer の NetworkManager からプレイヤー名や精度(%)を安全に取得するブリッジ
    /// </summary>
    public static class MultiplayerPlusBridge
    {
        private static Type _networkManagerType;
        private static PropertyInfo _statusProp;
        private static PropertyInfo _selfPlayerProp;
        private static PropertyInfo _roomDataProp;
        private static FieldInfo _roomDataField;
        private static PropertyInfo _roomPlayersProp;
        private static FieldInfo _roomPlayersDictField;
        private static FieldInfo _userNameField;
        private static FieldInfo _luidField;
        private static FieldInfo _accuracyField;
        private static FieldInfo _spectateField;
        private static FieldInfo _roomCodeField;
        private static FieldInfo _roomIdField;
        private static FieldInfo _hostLuidField;
        private static bool _initialized = false;

        private static readonly Dictionary<uint, string> _luidNameCache = new Dictionary<uint, string>();

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _networkManagerType = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Network.NetworkManager");
            if (_networkManagerType != null)
            {
                _statusProp = AccessTools.Property(_networkManagerType, "Status");
                _selfPlayerProp = AccessTools.Property(_networkManagerType, "SelfPlayer");
                _roomDataProp = AccessTools.Property(_networkManagerType, "RoomData");
                _roomDataField = AccessTools.Field(_networkManagerType, "m_RoomData");
                _roomPlayersProp = AccessTools.Property(_networkManagerType, "RoomPlayersLUnsafe");
                _roomPlayersDictField = AccessTools.Field(_networkManagerType, "m_RoomPlayersD");
            }

            var playerDataType = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Network.Types.PlayerData");
            if (playerDataType != null)
            {
                _userNameField = AccessTools.Field(playerDataType, "UserName");
                _luidField = AccessTools.Field(playerDataType, "LUID");
                _accuracyField = AccessTools.Field(playerDataType, "Accuracy");
                _spectateField = AccessTools.Field(playerDataType, "Spectate");
            }

            var roomDataType = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Network.Types.RoomData");
            if (roomDataType != null)
            {
                _roomCodeField = AccessTools.Field(roomDataType, "RoomCode");
                _roomIdField = AccessTools.Field(roomDataType, "RoomID");
                _hostLuidField = AccessTools.Field(roomDataType, "HostLUID");
            }
        }

        private static object GetRoomDataObject()
        {
            EnsureInitialized();
            if (_roomDataProp != null)
            {
                var rd = _roomDataProp.GetValue(null);
                if (rd != null) return rd;
            }
            if (_roomDataField != null)
            {
                return _roomDataField.GetValue(null);
            }
            return null;
        }

        public static string GetCurrentRoomCode()
        {
            try
            {
                var roomData = GetRoomDataObject();
                if (roomData != null)
                {
                    string code = _roomCodeField?.GetValue(roomData) as string;
                    if (!string.IsNullOrWhiteSpace(code))
                        return code.Trim().ToUpperInvariant();

                    string roomId = _roomIdField?.GetValue(roomData) as string;
                    if (!string.IsNullOrWhiteSpace(roomId))
                        return roomId.Trim().ToUpperInvariant();
                }
            }
            catch { }
            return "";
        }

        public static bool IsLocalPlayerRoomHost()
        {
            try
            {
                var roomData = GetRoomDataObject();
                var selfPlayer = _selfPlayerProp?.GetValue(null);
                if (roomData != null && selfPlayer != null && _hostLuidField != null && _luidField != null)
                {
                    uint hostLuid = Convert.ToUInt32(_hostLuidField.GetValue(roomData));
                    uint selfLuid = Convert.ToUInt32(_luidField.GetValue(selfPlayer));
                    if (hostLuid != 0 && selfLuid != 0)
                    {
                        return hostLuid == selfLuid;
                    }
                }
            }
            catch { }
            return true;
        }

        public static string GetRoomHostName()
        {
            try
            {
                if (IsLocalPlayerRoomHost())
                    return GetLocalPlayerName();

                var roomData = GetRoomDataObject();
                if (roomData != null && _hostLuidField != null)
                {
                    uint hostLuid = Convert.ToUInt32(_hostLuidField.GetValue(roomData));
                    if (hostLuid != 0)
                    {
                        string name = GetPlayerNameByLUID(hostLuid);
                        if (!string.IsNullOrEmpty(name)) return name;
                    }
                }
            }
            catch { }
            return "Host";
        }

        /// <summary>
        /// Multiplayer+ のルームに入室中かどうか (ENetworkStatus.InRoom == 6)
        /// </summary>
        public static bool IsInMultiplayerPlusRoom()
        {
            try
            {
                EnsureInitialized();
                if (_statusProp != null)
                {
                    object statusVal = _statusProp.GetValue(null);
                    if (statusVal != null && Convert.ToInt32(statusVal) == 6)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static string GetLocalPlayerName()
        {
            try
            {
                EnsureInitialized();
                if (_selfPlayerProp != null && _userNameField != null)
                {
                    var selfPlayer = _selfPlayerProp.GetValue(null);
                    if (selfPlayer != null)
                    {
                        string name = _userNameField.GetValue(selfPlayer) as string;
                        if (!string.IsNullOrEmpty(name)) return name;
                    }
                }
            }
            catch { }
            return "You";
        }

        public static string GetPlayerNameByLUID(uint luid)
        {
            try
            {
                EnsureInitialized();

                // 1. ConcurrentDictionary (m_RoomPlayersD) からスレッドセーフに取得
                if (_roomPlayersDictField != null && _userNameField != null)
                {
                    if (_roomPlayersDictField.GetValue(null) is IDictionary dict && dict.Contains(luid))
                    {
                        var item = dict[luid];
                        if (item != null)
                        {
                            string name = _userNameField.GetValue(item) as string;
                            if (!string.IsNullOrEmpty(name))
                            {
                                _luidNameCache[luid] = name;
                                return name;
                            }
                        }
                    }
                }

                // 2. RoomPlayersLUnsafe から取得
                if (_roomPlayersProp != null && _luidField != null && _userNameField != null)
                {
                    var list = _roomPlayersProp.GetValue(null) as IList;
                    if (list != null)
                    {
                        int count = list.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var item = list[i];
                            if (item == null) continue;
                            if (_luidField.GetValue(item) is uint itemLuid && itemLuid == luid)
                            {
                                string name = _userNameField.GetValue(item) as string;
                                if (!string.IsNullOrEmpty(name))
                                {
                                    _luidNameCache[luid] = name;
                                    return name;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            if (_luidNameCache.TryGetValue(luid, out string cachedName))
            {
                return cachedName;
            }
            return null;
        }

        public static void CollectRoomScores(List<(string Name, float Percent)> output)
        {
            try
            {
                EnsureInitialized();
                if (_userNameField == null || _accuracyField == null) return;

                // SelfPlayer
                if (_selfPlayerProp != null)
                {
                    var self = _selfPlayerProp.GetValue(null);
                    AddPlayerDataScore(self, output);
                }

                // Other Room Players via ConcurrentDictionary (m_RoomPlayersD)
                if (_roomPlayersDictField != null && _roomPlayersDictField.GetValue(null) is IDictionary dict)
                {
                    foreach (var val in dict.Values)
                    {
                        AddPlayerDataScore(val, output);
                    }
                    return;
                }

                // Fallback: RoomPlayersLUnsafe
                if (_roomPlayersProp != null && _roomPlayersProp.GetValue(null) is IList list)
                {
                    int count = list.Count;
                    for (int i = 0; i < count; i++)
                    {
                        AddPlayerDataScore(list[i], output);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in CollectRoomScores: " + ex);
            }
        }

        private static void AddPlayerDataScore(object playerData, List<(string Name, float Percent)> output)
        {
            if (playerData == null) return;
            if (_spectateField != null && _spectateField.GetValue(playerData) is bool spec && spec) return;

            string name = _userNameField?.GetValue(playerData) as string;
            if (string.IsNullOrEmpty(name)) return;

            if (_accuracyField?.GetValue(playerData) is float acc && acc >= 0f)
            {
                float pct = acc <= 1.0f ? acc * 100f : acc;
                int existingIdx = output.FindIndex(x => x.Name == name);
                if (existingIdx < 0)
                {
                    output.Add((name, pct));
                }
                else if (output[existingIdx].Percent <= 0f && pct > 0f)
                {
                    output[existingIdx] = (name, pct);
                }
            }
        }
    }
}
