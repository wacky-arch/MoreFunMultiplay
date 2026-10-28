using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using HarmonyLib;
using MoreFunMultiplay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoreFunMultiplay
{
    public class ResultsUIHost
    {
        public static ResultsUIHost Instance { get; } = new ResultsUIHost();

        private FloatingScreen _floatingScreen = null;
        private bool _isCollapsed = false;
        private int _currentTab = 0;

        private readonly string[] _tabContents = new string[5]
        {
            "<color=#FFD700>🏆 本日の総合ランキング 🏆</color>\nまだ記録がありません",
            "<color=#00E5FF>🌐 通算の総合ランキング 🌐</color>\nまだ記録がありません",
            "<color=#FF4400>🔥 運動量ランキング 🔥</color>\nまだ記録がありません",
            "<color=#00FFFF>🗿 地蔵スタイル賞 🗿</color>\nまだ記録がありません",
            "<color=#FF00FF>🎯 チキンレース 🎯</color>\nまだ記録がありません"
        };

        private readonly string[] _lobbyTabContents = new string[5]
        {
            "<color=#FFD700>🏆 本日の総合ランキング 🏆</color>\nまだ記録がありません",
            "<color=#00E5FF>🌐 通算の総合ランキング 🌐</color>\nまだ記録がありません",
            "<color=#FF4400>🔥 運動量ランキング 🔥</color>\nまだ記録がありません",
            "<color=#00FFFF>🗿 地蔵スタイル賞 🗿</color>\nまだ記録がありません",
            "<color=#FF00FF>🎯 チキンレース 🎯</color>\nまだ記録がありません"
        };

        private readonly int[] _tabItemCounts = new int[5];
        private readonly Dictionary<string, int> _lastEarnedPoints = new Dictionary<string, int>();
        private List<(string Name, float Percent)> _lastCompletedScores = new List<(string Name, float Percent)>();

        private bool _hasProcessedCurrentSong = true;

        [UIComponent("top-bar")]
        private RectTransform _topBar = null;

        [UIComponent("header-text")]
        private TextMeshProUGUI _headerText = null;

        [UIComponent("main-body")]
        private RectTransform _mainBody = null;

        [UIComponent("tab-row-1")]
        private RectTransform _tabRow1 = null;

        [UIComponent("tab-row-2")]
        private RectTransform _tabRow2 = null;

        [UIComponent("result-scroll")]
        private HMUI.ScrollView _resultScrollView = null;

        [UIComponent("result-text")]
        private TextMeshProUGUI _resultText = null;

        [UIComponent("collapse-btn")]
        private Button _collapseBtn = null;

        private static FieldInfo _contentRectField;
        private static FieldInfo _pageUpBtnField;
        private static FieldInfo _pageDownBtnField;
        private static FieldInfo _scrollIndicatorField;

        public bool IsFloatingScreenVisible => _floatingScreen != null && _floatingScreen.gameObject.activeSelf;

        [UIAction("toggle-collapse")]
        public void OnToggleCollapse()
        {
            _isCollapsed = !_isCollapsed;
            if (PluginConfig.Instance != null)
            {
                PluginConfig.Instance.IsResultsCollapsed = _isCollapsed;
            }
            ApplyCollapseState();
            if (!_isCollapsed)
            {
                RefreshDisplayedText();
            }
        }

        [UIAction("tab-0")]
        public void OnTab0() => SwitchTab(0);

        [UIAction("tab-1")]
        public void OnTab1() => SwitchTab(1);

        [UIAction("tab-2")]
        public void OnTab2() => SwitchTab(2);

        [UIAction("tab-3")]
        public void OnTab3() => SwitchTab(3);

        [UIAction("tab-4")]
        public void OnTab4() => SwitchTab(4);

        public void MarkSongStarted()
        {
            _hasProcessedCurrentSong = false;
        }

        public void ClearLastEarnedPoints()
        {
            _lastEarnedPoints.Clear();
        }

        public string GetTabContent(int index)
        {
            if (index >= 0 && index < _tabContents.Length)
                return _tabContents[index];
            return "";
        }

        public string GetLobbyTabContent(int index)
        {
            if (index >= 0 && index < _lobbyTabContents.Length)
                return _lobbyTabContents[index];
            return "";
        }

        public void SwitchTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabContents.Length) return;
            _currentTab = tabIndex;
            RefreshDisplayedText();
        }

        private void RefreshDisplayedText()
        {
            if (_resultText != null && _currentTab >= 0 && _currentTab < _tabContents.Length)
            {
                _resultText.alignment = TextAlignmentOptions.Top;
                _resultText.text = _tabContents[_currentTab];
                _resultText.SetAllDirty();
                if (_resultText.gameObject.activeInHierarchy)
                {
                    _resultText.ForceMeshUpdate();
                }
            }

            if (_resultScrollView != null)
            {
                EnsureScrollViewFieldsCached();

                bool needsScroll = (_currentTab >= 0 && _currentTab < _tabItemCounts.Length) && (_tabItemCounts[_currentTab] >= 20);

                var pageUp = _pageUpBtnField?.GetValue(_resultScrollView) as Component;
                var pageDown = _pageDownBtnField?.GetValue(_resultScrollView) as Component;
                var indicator = _scrollIndicatorField?.GetValue(_resultScrollView) as Component;

                if (pageUp != null) pageUp.gameObject.SetActive(needsScroll);
                if (pageDown != null) pageDown.gameObject.SetActive(needsScroll);
                if (indicator != null) indicator.gameObject.SetActive(needsScroll);

                var content = _contentRectField?.GetValue(_resultScrollView) as RectTransform;
                if (content != null)
                {
                    foreach (var vlg in content.GetComponentsInChildren<VerticalLayoutGroup>(true))
                    {
                        vlg.childAlignment = TextAnchor.UpperCenter;
                        vlg.childForceExpandHeight = false;
                        vlg.childControlHeight = true;
                    }
                    LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                    content.anchoredPosition = Vector2.zero;
                }

                if (needsScroll)
                {
                    _resultScrollView.enabled = true;
                    _resultScrollView.UpdateContentSize();
                    _resultScrollView.ScrollTo(0f, false);
                }
                else
                {
                    _resultScrollView.ScrollTo(0f, false);
                    _resultScrollView.enabled = false;
                    if (content != null)
                    {
                        content.anchoredPosition = Vector2.zero;
                    }
                }
            }

            SettingsUI.Instance.OnExternalTabSwitched(_currentTab);
        }

        private static void EnsureScrollViewFieldsCached()
        {
            if (_contentRectField != null) return;
            var svType = typeof(HMUI.ScrollView);
            _contentRectField = AccessTools.Field(svType, "_contentRectTransform");
            _pageUpBtnField = AccessTools.Field(svType, "_pageUpButton");
            _pageDownBtnField = AccessTools.Field(svType, "_pageDownButton");
            _scrollIndicatorField = AccessTools.Field(svType, "_verticalScrollIndicator");
        }

        private void ApplyCollapseState()
        {
            if (_mainBody != null)
            {
                _mainBody.gameObject.SetActive(!_isCollapsed);
            }
            if (_collapseBtn != null)
            {
                var tmp = _collapseBtn.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = _isCollapsed ? "表示 ▼" : "非表示 ▲";
                }
            }
        }

        /// <summary>
        /// ホスト（自分）が曲を最後までプレイできたかどうかを判定
        /// </summary>
        public static bool IsSongCompletedToEnd(LevelCompletionResults results)
        {
            if (results == null) return false;
            return results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared
                && results.levelEndAction == LevelCompletionResults.LevelEndAction.None;
        }

        public void ProcessSongEndIfNeeded(LevelCompletionResults results)
        {
            if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return;

            var manager = MultiplayManager.Instance;
            if (manager == null) return;

            if (!_hasProcessedCurrentSong)
            {
                _hasProcessedCurrentSong = true;
                bool isHost = MultiplayerPlusBridge.IsLocalPlayerRoomHost();

                if (isHost)
                {
                    if (IsSongCompletedToEnd(results))
                    {
                        manager.CompleteSong();
                        _lastCompletedScores = CollectLiveScores();
                        CalculateAndAwardPoints();

                        int defaultTab = 0;
                        var mode = (GameMode)PluginConfig.Instance.SelectedGameMode;
                        if (mode == GameMode.Sweat) defaultTab = 2;
                        else if (mode == GameMode.Jizo) defaultTab = 3;
                        else if (mode == GameMode.ChickenRace) defaultTab = 4;
                        _currentTab = defaultTab;

                        UpdateTabContents();
                        RefreshDisplayedText();
                        PublishCurrentStateToCloud(true);
                        return;
                    }
                    else
                    {
                        // 曲が途中で終了（中断・Quit・途中失敗）した場合は点数を付けず、今回の途中データを破棄
                        manager.AbortSong();
                        Plugin.Log.Info($"Song ended midway (State={results?.levelEndStateType}, Action={results?.levelEndAction}). Points not awarded.");
                    }
                }
                else
                {
                    // 参加者モード: 自分のSessionStoreには加算せず、ホストが公開した最新ランキングを受信する
                    manager.CompleteSong();
                    _lastCompletedScores = CollectLiveScores();
                    string roomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
                    RoomSyncManager.TriggerParticipantFetchWithRetries(roomCode);
                }
            }
            else if (manager.IsInSong)
            {
                manager.StopSong();
            }

            if (MultiplayerPlusBridge.IsLocalPlayerRoomHost())
            {
                UpdateTabContents();
            }
            UpdateHeaderText();
            RefreshDisplayedText();
        }

        public void UpdateHeaderText()
        {
            if (_headerText == null) return;
            string roomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
            if (!MultiplayerPlusBridge.IsLocalPlayerRoomHost())
            {
                string hostName = MultiplayerPlusBridge.GetRoomHostName();
                _headerText.text = string.IsNullOrEmpty(roomCode)
                    ? $"🏆 MFM (Host: {hostName})"
                    : $"🏆 MFM [{roomCode}]";
            }
            else
            {
                _headerText.text = string.IsNullOrEmpty(roomCode)
                    ? "🏆 MoreFunMultiplay"
                    : $"🏆 MFM [Code: {roomCode}]";
            }
        }

        public void PublishCurrentStateToCloud(bool isSongEndUpdate)
        {
            if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return;
            if (!MultiplayerPlusBridge.IsLocalPlayerRoomHost()) return;

            string roomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
            if (string.IsNullOrEmpty(roomCode)) return;

            var snapshot = new RoomRankingSnapshot
            {
                Version = 1,
                RoomCode = roomCode,
                HostName = MultiplayerPlusBridge.GetLocalPlayerName(),
                SelectedGameMode = PluginConfig.Instance != null ? PluginConfig.Instance.SelectedGameMode : 0,
                TargetScorePercentage = PluginConfig.Instance != null ? PluginConfig.Instance.TargetScorePercentage : 90f,
                RecommendedTab = _currentTab,
                DailyMatchesPlayed = SessionStore.Current.DailyMatchesPlayed,
                TotalMatchesPlayed = SessionStore.Current.TotalMatchesPlayed,
                IsSongEndUpdate = isSongEndUpdate
            };

            for (int i = 0; i < 5; i++)
            {
                snapshot.TabContents[i] = _tabContents[i] ?? "";
                snapshot.LobbyTabContents[i] = _lobbyTabContents[i] ?? "";
                snapshot.TabItemCounts[i] = _tabItemCounts[i];
            }

            RoomSyncManager.PublishHostSnapshot(snapshot, isSongEndUpdate);
        }

        public void ApplyRemoteSnapshot(RoomRankingSnapshot snapshot)
        {
            if (snapshot == null) return;
            if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return;
            if (MultiplayerPlusBridge.IsLocalPlayerRoomHost()) return;

            if (snapshot.TabContents != null && snapshot.TabContents.Length == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (!string.IsNullOrEmpty(snapshot.TabContents[i]))
                        _tabContents[i] = snapshot.TabContents[i];
                }
            }

            if (snapshot.LobbyTabContents != null && snapshot.LobbyTabContents.Length == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (!string.IsNullOrEmpty(snapshot.LobbyTabContents[i]))
                        _lobbyTabContents[i] = snapshot.LobbyTabContents[i];
                }
            }

            if (snapshot.TabItemCounts != null && snapshot.TabItemCounts.Length == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    _tabItemCounts[i] = snapshot.TabItemCounts[i];
                }
            }

            if (snapshot.IsSongEndUpdate && snapshot.RecommendedTab >= 0 && snapshot.RecommendedTab < 5)
            {
                _currentTab = snapshot.RecommendedTab;
            }

            UpdateHeaderText();
            RefreshDisplayedText();
        }

        public static int GetRankPoints(int rank, int totalPlayers)
        {
            if (totalPlayers <= 0 || rank <= 0 || rank > totalPlayers) return 0;
            int basePoints = totalPlayers - rank + 1;
            int bonus = (rank == 1) ? 2 : (rank == 2 ? 1 : 0);
            return basePoints + bonus;
        }

        private float GetPlayerAccuracy(string playerName, List<(string Name, float Percent)> scores)
        {
            if (scores != null && !string.IsNullOrEmpty(playerName))
            {
                int idx = scores.FindIndex(s => string.Equals(s.Name, playerName, StringComparison.OrdinalIgnoreCase) || s.Name == playerName);
                if (idx >= 0)
                {
                    return Mathf.Clamp(scores[idx].Percent, 0f, 100f);
                }
            }
            return 0f;
        }

        private void CalculateAndAwardPoints()
        {
            if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return;

            var manager = MultiplayManager.Instance;
            if (manager == null) return;

            var players = manager.GetTrackedPlayers();
            var scores = ExtractAllScores();
            var currentMode = (GameMode)PluginConfig.Instance.SelectedGameMode;

            _lastEarnedPoints.Clear();
            List<(string Name, float Value)> rankedList = null;

            if (currentMode == GameMode.Sweat && players != null && players.Count > 0)
            {
                rankedList = players.Values
                    .Select(p =>
                    {
                        float acc = GetPlayerAccuracy(p.PlayerName, scores);
                        float eval = p.TotalDistance * (acc / 100f);
                        return (Name: p.PlayerName, Eval: eval, Acc: acc, Dist: p.TotalDistance);
                    })
                    .OrderByDescending(x => x.Eval)
                    .ThenByDescending(x => x.Acc)
                    .ThenByDescending(x => x.Dist)
                    .Select(x => (x.Name, x.Eval))
                    .ToList();
            }
            else if (currentMode == GameMode.Jizo && players != null && players.Count > 0)
            {
                rankedList = players.Values
                    .Select(p =>
                    {
                        float acc = GetPlayerAccuracy(p.PlayerName, scores);
                        float eval = acc > 0.001f ? (p.HeadDistance / (acc / 100f)) : float.MaxValue;
                        return (Name: p.PlayerName, Eval: eval, Acc: acc, HeadDist: p.HeadDistance);
                    })
                    .OrderBy(x => x.Eval)
                    .ThenByDescending(x => x.Acc)
                    .ThenBy(x => x.HeadDist)
                    .Select(x => (x.Name, x.Eval))
                    .ToList();
            }
            else if (currentMode == GameMode.ChickenRace && scores != null && scores.Count > 0)
            {
                float target = PluginConfig.Instance.TargetScorePercentage;
                rankedList = scores
                    .OrderBy(s => Math.Abs(s.Percent - target))
                    .Select(s => (s.Name, s.Percent))
                    .ToList();
            }

            if (rankedList != null && rankedList.Count > 0)
            {
                int n = rankedList.Count;
                for (int k = 0; k < n; k++)
                {
                    int rank = k + 1;
                    int totalPoints = GetRankPoints(rank, n);

                    string playerName = rankedList[k].Name;
                    _lastEarnedPoints[playerName] = totalPoints;
                    SessionStore.AddPoints(playerName, totalPoints);
                }

                SessionStore.Current.DailyMatchesPlayed++;
                SessionStore.Current.TotalMatchesPlayed++;
                SessionStore.Save();
                Plugin.Log.Info($"Awarded points to {n} players in Multiplayer+. DailyMatches: {SessionStore.Current.DailyMatchesPlayed}, TotalMatches: {SessionStore.Current.TotalMatchesPlayed}");
            }
        }

        public void UpdateTabContents()
        {
            var manager = MultiplayManager.Instance;
            var players = manager != null ? manager.GetTrackedPlayers() : null;
            var scores = ExtractAllScores();
            var currentMode = (GameMode)PluginConfig.Instance.SelectedGameMode;

            // 0. Daily Ranking
            string dailyTitle = $"<color=#FFD700>🏆 本日の総合 (本日: {SessionStore.Current.DailyMatchesPlayed}曲)</color>\n";
            string dailyBody = "";
            string dailyLobby = "";
            _tabItemCounts[0] = SessionStore.Current.DailyScores.Count;
            if (SessionStore.Current.DailyScores.Count > 0)
            {
                var sortedDaily = SessionStore.Current.DailyScores.OrderByDescending(s => s.Value).ToList();
                for (int i = 0; i < sortedDaily.Count; i++)
                {
                    string medal = i == 0 ? "<color=#FFD700>1位</color>" : (i == 1 ? "<color=#E0E0E0>2位</color>" : (i == 2 ? "<color=#CD7F32>3位</color>" : $"{i + 1}位"));
                    string name = sortedDaily[i].Key;
                    string gainedStr = (_lastEarnedPoints.TryGetValue(name, out int gained) && gained > 0 && SessionStore.Current.DailyMatchesPlayed > 0)
                        ? $" <color=#FFD700>(+{gained}pt)</color>"
                        : "";
                    string line = $"{medal} {name}: <color=#00FF88>{sortedDaily[i].Value} pt</color>{gainedStr}\n";
                    dailyBody += line;
                    if (i < 5) dailyLobby += line;
                }
            }
            else
            {
                dailyBody = "本日の得点記録はまだありません\n";
                dailyLobby = "本日の得点記録はまだありません\n";
            }
            _tabContents[0] = dailyTitle + dailyBody;
            _lobbyTabContents[0] = dailyTitle + dailyLobby;

            // 1. Total Ranking
            string totalTitle = $"<color=#00E5FF>🌐 通算の総合 (通算: {SessionStore.Current.TotalMatchesPlayed}曲)</color>\n";
            string totalBody = "";
            string totalLobby = "";
            _tabItemCounts[1] = SessionStore.Current.TotalScores.Count;
            if (SessionStore.Current.TotalScores.Count > 0)
            {
                var sortedTotal = SessionStore.Current.TotalScores.OrderByDescending(s => s.Value).ToList();
                for (int i = 0; i < sortedTotal.Count; i++)
                {
                    string medal = i == 0 ? "<color=#FFD700>1位</color>" : (i == 1 ? "<color=#E0E0E0>2位</color>" : (i == 2 ? "<color=#CD7F32>3位</color>" : $"{i + 1}位"));
                    string name = sortedTotal[i].Key;
                    string gainedStr = (_lastEarnedPoints.TryGetValue(name, out int gained) && gained > 0 && SessionStore.Current.TotalMatchesPlayed > 0)
                        ? $" <color=#FFD700>(+{gained}pt)</color>"
                        : "";
                    string line = $"{medal} {name}: <color=#00FF88>{sortedTotal[i].Value} pt</color>{gainedStr}\n";
                    totalBody += line;
                    if (i < 5) totalLobby += line;
                }
            }
            else
            {
                totalBody = "通算の得点記録はまだありません\n";
                totalLobby = "通算の得点記録はまだありません\n";
            }
            _tabContents[1] = totalTitle + totalBody;
            _lobbyTabContents[1] = totalTitle + totalLobby;

            // 2. Sweat Ranking (運動量 × 精度% ÷ 100 で大きい順)
            string sweatTitle = "<color=#FF4400>🔥 運動量 (移動×精度%)</color>";
            if (currentMode == GameMode.Sweat) sweatTitle += " <color=#FFD700>[★勝負]</color>";
            sweatTitle += "\n";
            string sweatBody = "";
            string sweatLobby = "";
            _tabItemCounts[2] = players != null ? players.Count : 0;
            if (players != null && players.Count > 0)
            {
                var sorted = players.Values
                    .Select(p =>
                    {
                        float acc = GetPlayerAccuracy(p.PlayerName, scores);
                        float eval = p.TotalDistance * (acc / 100f);
                        return (Name: p.PlayerName, Dist: p.TotalDistance, Acc: acc, Eval: eval);
                    })
                    .OrderByDescending(x => x.Eval)
                    .ThenByDescending(x => x.Acc)
                    .ThenByDescending(x => x.Dist)
                    .ToList();

                int n = sorted.Count;
                for (int i = 0; i < n; i++)
                {
                    int rank = i + 1;
                    int pts = GetRankPoints(rank, n);
                    string medal = i == 0 ? "<color=#FFD700>1位</color>" : (i == 1 ? "<color=#E0E0E0>2位</color>" : (i == 2 ? "<color=#CD7F32>3位</color>" : $"{rank}位"));
                    string line = $"{medal} {sorted[i].Name}: <color=#FFCC00>{sorted[i].Eval:F1}</color> ({sorted[i].Dist:F1}m×{sorted[i].Acc:F1}%) <color=#00FF88>+{pts}pt</color>\n";
                    sweatBody += line;
                    if (i < 5) sweatLobby += line;
                }
            }
            else
            {
                sweatBody = "直近の曲データなし\n";
                sweatLobby = "直近の曲データなし\n";
            }
            _tabContents[2] = sweatTitle + sweatBody;
            _lobbyTabContents[2] = sweatTitle + sweatLobby;

            // 3. Jizo Ranking (頭移動 ÷ (精度% ÷ 100) で小さい順)
            string jizoTitle = "<color=#00FFFF>🗿 地蔵 (頭移動÷精度%・小順)</color>";
            if (currentMode == GameMode.Jizo) jizoTitle += " <color=#FFD700>[★勝負]</color>";
            jizoTitle += "\n";
            string jizoBody = "";
            string jizoLobby = "";
            _tabItemCounts[3] = players != null ? players.Count : 0;
            if (players != null && players.Count > 0)
            {
                var sorted = players.Values
                    .Select(p =>
                    {
                        float acc = GetPlayerAccuracy(p.PlayerName, scores);
                        bool validAcc = acc > 0.001f;
                        float eval = validAcc ? (p.HeadDistance / (acc / 100f)) : float.MaxValue;
                        return (Name: p.PlayerName, HeadDist: p.HeadDistance, Acc: acc, ValidAcc: validAcc, Eval: eval);
                    })
                    .OrderBy(x => x.Eval)
                    .ThenByDescending(x => x.Acc)
                    .ThenBy(x => x.HeadDist)
                    .ToList();

                int n = sorted.Count;
                for (int i = 0; i < n; i++)
                {
                    int rank = i + 1;
                    int pts = GetRankPoints(rank, n);
                    string medal = i == 0 ? "<color=#FFD700>1位</color>" : (i == 1 ? "<color=#E0E0E0>2位</color>" : (i == 2 ? "<color=#CD7F32>3位</color>" : $"{rank}位"));
                    string evalStr = sorted[i].ValidAcc ? $"<color=#00FFFF>{sorted[i].Eval:F1}</color>" : "<color=#888888>記録なし</color>";
                    string line = $"{medal} {sorted[i].Name}: {evalStr} ({sorted[i].HeadDist:F1}m÷{sorted[i].Acc:F1}%) <color=#00FF88>+{pts}pt</color>\n";
                    jizoBody += line;
                    if (i < 5) jizoLobby += line;
                }
            }
            else
            {
                jizoBody = "直近の曲データなし\n";
                jizoLobby = "直近の曲データなし\n";
            }
            _tabContents[3] = jizoTitle + jizoBody;
            _lobbyTabContents[3] = jizoTitle + jizoLobby;

            // 4. Chicken Race Ranking
            float target = PluginConfig.Instance.TargetScorePercentage;
            string chickenTitle = $"<color=#FF00FF>🎯 チキンレース (目標: {target:F0}%)</color>";
            if (currentMode == GameMode.ChickenRace) chickenTitle += " <color=#FFD700>[★勝負]</color>";
            chickenTitle += "\n";
            string chickenBody = "";
            string chickenLobby = "";
            _tabItemCounts[4] = scores != null ? scores.Count : 0;
            if (scores != null && scores.Count > 0)
            {
                var sorted = scores.OrderBy(s => Math.Abs(s.Percent - target)).ToList();
                int n = sorted.Count;
                for (int i = 0; i < n; i++)
                {
                    int rank = i + 1;
                    int pts = GetRankPoints(rank, n);
                    float diff = Math.Abs(sorted[i].Percent - target);
                    string medal = i == 0 ? "<color=#FFD700>1位</color>" : (i == 1 ? "<color=#E0E0E0>2位</color>" : (i == 2 ? "<color=#CD7F32>3位</color>" : $"{rank}位"));
                    string line = $"{medal} {sorted[i].Name}: {sorted[i].Percent:F2}% (差 {diff:F2}%) <color=#00FF88>+{pts}pt</color>\n";
                    chickenBody += line;
                    if (i < 5) chickenLobby += line;
                }
            }
            else
            {
                chickenBody = "直近のスコアデータなし\n";
                chickenLobby = "直近のスコアデータなし\n";
            }
            _tabContents[4] = chickenTitle + chickenBody;
            _lobbyTabContents[4] = chickenTitle + chickenLobby;

            RefreshDisplayedText();
        }

        private List<(string Name, float Percent)> CollectLiveScores()
        {
            var output = new List<(string Name, float Percent)>();
            if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return output;

            // 1. Multiplayer+ (BeatSaberPlus_Multiplayer) のルームスコアを取得
            MultiplayerPlusBridge.CollectRoomScores(output);

            // 2. ローカルプレイヤーの確定スコア精度(%)を補完
            if (MultiplayManager.Instance != null)
            {
                float acc = MultiplayManager.Instance.LastCompletedLocalAccuracyPercent >= 0f
                    ? MultiplayManager.Instance.LastCompletedLocalAccuracyPercent
                    : MultiplayManager.Instance.LocalPlayerAccuracyPercent;
                if (acc >= 0f)
                {
                    string localName = MultiplayManager.Instance.LocalPlayerName ?? MultiplayerPlusBridge.GetLocalPlayerName();
                    int existingIdx = output.FindIndex(x => x.Name == localName);
                    if (existingIdx < 0)
                    {
                        output.Add((localName, acc));
                    }
                    else if (output[existingIdx].Percent <= 0f && acc > 0f)
                    {
                        output[existingIdx] = (localName, acc);
                    }
                }
            }

            return output;
        }

        private List<(string Name, float Percent)> ExtractAllScores()
        {
            if (_lastCompletedScores != null && _lastCompletedScores.Count > 0)
            {
                return _lastCompletedScores;
            }
            return CollectLiveScores();
        }

        private void EnsureFloatingScreenCreated()
        {
            if (_floatingScreen != null) return;

            var cfg = PluginConfig.Instance;
            Vector3 pos = cfg != null
                ? new Vector3(cfg.FloatingPosX, cfg.FloatingPosY, cfg.FloatingPosZ)
                : new Vector3(0f, 2.25f, 2.5f);
            Quaternion rot = cfg != null
                ? Quaternion.Euler(cfg.FloatingRotX, cfg.FloatingRotY, cfg.FloatingRotZ)
                : Quaternion.Euler(-12f, 0f, 0f);

            _isCollapsed = cfg != null && cfg.IsResultsCollapsed;

            // 横幅をさらに狭くスリム化 (幅72, 高さ86) し、タブボタンは2段の大きめボタンに配置
            _floatingScreen = FloatingScreen.CreateFloatingScreen(
                new Vector2(72f, 86f),
                createHandle: true,
                position: pos,
                rotation: rot,
                curvatureRadius: 0f,
                hasBackground: false);

            _floatingScreen.ShowHandle = true;
            _floatingScreen.HighlightHandle = true;
            _floatingScreen.HandleSide = FloatingScreen.Side.Top;
            _floatingScreen.gameObject.name = "MoreFunMultiplayFloatingScreen";
            UnityEngine.Object.DontDestroyOnLoad(_floatingScreen.gameObject);

            var handleGo = AccessTools.Field(typeof(FloatingScreen), "handle")?.GetValue(_floatingScreen) as GameObject;
            if (handleGo != null)
            {
                handleGo.transform.localPosition = new Vector3(0f, 43.5f, 0f);
                handleGo.transform.localScale = new Vector3(64f, 4.0f, 4.0f);
            }

            _floatingScreen.HandleReleased -= OnFloatingScreenHandleReleased;
            _floatingScreen.HandleReleased += OnFloatingScreenHandleReleased;

            string bsml = @"<bg xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='https://monkeymanboy.github.io/BSML-Docs/ https://raw.githubusercontent.com/monkeymanboy/BSML-Docs/gh-pages/BSMLSchema.xsd'>
  <horizontal id='top-bar' bg='round-rect-panel' pad='1' spacing='2' pref-width='70' pref-height='8.5' child-align='MiddleCenter'>
    <text id='header-text' text='🏆 MoreFunMultiplay' font-size='2.8' align='Center' rich-text='true'/>
    <button id='collapse-btn' text='非表示 ▲' on-click='toggle-collapse' pref-width='22' pref-height='6.8' font-size='2.8'/>
  </horizontal>
  <vertical id='main-body' bg='round-rect-panel' pad='1' spacing='1' pref-width='70' pref-height='73'>
    <horizontal id='tab-row-1' spacing='2' horizontal-fit='PreferredSize' child-align='MiddleCenter'>
      <button text='🏆 当日総合' on-click='tab-0' pref-width='31' pref-height='7.5' font-size='3.0'/>
      <button text='🌐 通算総合' on-click='tab-1' pref-width='31' pref-height='7.5' font-size='3.0'/>
    </horizontal>
    <horizontal id='tab-row-2' spacing='1.5' horizontal-fit='PreferredSize' child-align='MiddleCenter'>
      <button text='🔥 運動量' on-click='tab-2' pref-width='20.5' pref-height='7.5' font-size='2.9'/>
      <button text='🗿 地蔵' on-click='tab-3' pref-width='20.5' pref-height='7.5' font-size='2.9'/>
      <button text='🎯 チキン' on-click='tab-4' pref-width='20.5' pref-height='7.5' font-size='2.9'/>
    </horizontal>
    <scroll-view id='result-scroll' pref-width='66' pref-height='53'>
      <text id='result-text' text='読み込み中...' align='Top' font-size='2.45' rich-text='true'/>
    </scroll-view>
  </vertical>
</bg>";
            BSMLCompat.Parse(bsml, _floatingScreen.gameObject, this);
            FixupFloatingScreenLayout();
            ApplyCollapseState();
            UpdateHeaderText();
            if (MultiplayerPlusBridge.IsLocalPlayerRoomHost())
            {
                UpdateTabContents();
            }
        }

        private void FixupFloatingScreenLayout()
        {
            if (_topBar != null)
            {
                var csf = _topBar.GetComponent<ContentSizeFitter>();
                if (csf != null) csf.enabled = false;
                _topBar.anchorMin = new Vector2(0.5f, 0.5f);
                _topBar.anchorMax = new Vector2(0.5f, 0.5f);
                _topBar.pivot = new Vector2(0.5f, 0.5f);
                _topBar.anchoredPosition = new Vector2(0f, 36.5f);
                _topBar.sizeDelta = new Vector2(70f, 8.5f);
            }

            if (_mainBody != null)
            {
                var vlg = _mainBody.GetComponent<VerticalLayoutGroup>();
                if (vlg != null) vlg.enabled = false;
                var csf = _mainBody.GetComponent<ContentSizeFitter>();
                if (csf != null) csf.enabled = false;
                _mainBody.anchorMin = new Vector2(0.5f, 0.5f);
                _mainBody.anchorMax = new Vector2(0.5f, 0.5f);
                _mainBody.pivot = new Vector2(0.5f, 0.5f);
                _mainBody.anchoredPosition = new Vector2(0f, -5.5f);
                _mainBody.sizeDelta = new Vector2(70f, 73f);
            }

            if (_tabRow1 != null)
            {
                var csf = _tabRow1.GetComponent<ContentSizeFitter>();
                if (csf != null) csf.enabled = false;
                _tabRow1.anchorMin = new Vector2(0.5f, 0.5f);
                _tabRow1.anchorMax = new Vector2(0.5f, 0.5f);
                _tabRow1.pivot = new Vector2(0.5f, 0.5f);
                _tabRow1.anchoredPosition = new Vector2(0f, 30.5f);
                _tabRow1.sizeDelta = new Vector2(66f, 8f);
            }

            if (_tabRow2 != null)
            {
                var csf = _tabRow2.GetComponent<ContentSizeFitter>();
                if (csf != null) csf.enabled = false;
                _tabRow2.anchorMin = new Vector2(0.5f, 0.5f);
                _tabRow2.anchorMax = new Vector2(0.5f, 0.5f);
                _tabRow2.pivot = new Vector2(0.5f, 0.5f);
                _tabRow2.anchoredPosition = new Vector2(0f, 22.0f);
                _tabRow2.sizeDelta = new Vector2(66f, 8f);
            }

            if (_resultScrollView != null)
            {
                var rt = _resultScrollView.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, -9.0f);
                    rt.sizeDelta = new Vector2(66f, 53f);
                }
            }

            if (_resultText != null)
            {
                _resultText.overflowMode = TextOverflowModes.Overflow;
                _resultText.enableWordWrapping = false;
                _resultText.alignment = TextAlignmentOptions.Top;
                _resultText.fontSize = 2.45f;
            }
        }

        private void OnFloatingScreenHandleReleased(object sender, FloatingScreenHandleEventArgs args)
        {
            try
            {
                if (PluginConfig.Instance == null || _floatingScreen == null) return;
                Vector3 pos = _floatingScreen.ScreenPosition;
                Vector3 rot = _floatingScreen.ScreenRotation.eulerAngles;
                PluginConfig.Instance.FloatingPosX = pos.x;
                PluginConfig.Instance.FloatingPosY = pos.y;
                PluginConfig.Instance.FloatingPosZ = pos.z;
                PluginConfig.Instance.FloatingRotX = rot.x;
                PluginConfig.Instance.FloatingRotY = rot.y;
                PluginConfig.Instance.FloatingRotZ = rot.z;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error saving floating screen position: " + ex);
            }
        }

        public void ShowFloatingScreen()
        {
            if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
            {
                HideFloatingScreen();
                return;
            }
            EnsureFloatingScreenCreated();
            if (_floatingScreen != null)
            {
                _floatingScreen.gameObject.SetActive(true);
                FixupFloatingScreenLayout();
                ApplyCollapseState();
                UpdateHeaderText();
                if (MultiplayerPlusBridge.IsLocalPlayerRoomHost())
                {
                    UpdateTabContents();
                }
                else
                {
                    string roomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
                    if (!string.IsNullOrEmpty(roomCode))
                    {
                        RoomSyncManager.PollLatestRoomSnapshotAsync(roomCode);
                    }
                }
                RefreshDisplayedText();
            }
        }

        public void HideFloatingScreen()
        {
            if (_floatingScreen != null)
            {
                _floatingScreen.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 曲終了時に表示される ResultsViewController へのパッチ（Multiplayer+ ルーム内でのみ動作、1.29.1〜1.44.1互換）
    /// </summary>
    [HarmonyPatch]
    public class SoloAndMultiPlusResultsPatch
    {
        private static FieldInfo _resultsField;

        [HarmonyPrepare]
        static bool Prepare() => TargetMethod() != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("ResultsViewController");
            return type != null ? AccessTools.Method(type, "DidActivate") : null;
        }

        [HarmonyPostfix]
        static void Postfix(object __instance)
        {
            try
            {
                if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
                {
                    ResultsUIHost.Instance.HideFloatingScreen();
                    return;
                }

                if (_resultsField == null && __instance != null)
                {
                    _resultsField = AccessTools.Field(__instance.GetType(), "_levelCompletionResults");
                }
                var results = _resultsField?.GetValue(__instance) as LevelCompletionResults;

                ResultsUIHost.Instance.ProcessSongEndIfNeeded(results);
                ResultsUIHost.Instance.ShowFloatingScreen();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in SoloAndMultiPlusResultsPatch: " + ex);
            }
        }
    }

    /// <summary>
    /// メインメニューに戻った際、Multiplayer+ ルーム外であればフローティング画面を隠す（1.29.1〜1.44.1互換）
    /// </summary>
    [HarmonyPatch]
    public class MainMenuDeactivateFloatingScreenPatch
    {
        [HarmonyPrepare]
        static bool Prepare() => TargetMethod() != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("MainMenuViewController");
            return type != null ? AccessTools.Method(type, "DidActivate") : null;
        }

        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                Plugin.OnMainMenuInitializing();
                if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
                {
                    ResultsUIHost.Instance.HideFloatingScreen();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in MainMenuDeactivateFloatingScreenPatch: " + ex);
            }
        }
    }

    /// <summary>
    /// Multiplayer+ の GameplayManager.Logic_OnLevelEnded でホストが最後まで完走したか判定し集計＆表示を実行
    /// </summary>
    [HarmonyPatch]
    public class MultiplayerPlusLevelEndedPatch
    {
        private static PropertyInfo _resultsProp;

        [HarmonyPrepare]
        static bool Prepare() => TargetMethod() != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Managers.GameplayManager");
            if (type == null) return null;
            return AccessTools.Method(type, "Logic_OnLevelEnded");
        }

        [HarmonyPostfix]
        static void Postfix(object[] __args)
        {
            try
            {
                if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom()) return;

                LevelCompletionResults results = null;
                if (__args != null && __args.Length > 0 && __args[0] != null)
                {
                    if (_resultsProp == null)
                    {
                        _resultsProp = AccessTools.Property(__args[0].GetType(), "Results");
                    }
                    results = _resultsProp?.GetValue(__args[0]) as LevelCompletionResults;
                }

                ResultsUIHost.Instance.ProcessSongEndIfNeeded(results);
                ResultsUIHost.Instance.ShowFloatingScreen();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in MultiplayerPlusLevelEndedPatch: " + ex);
            }
        }
    }

    /// <summary>
    /// Multiplayer+ ルーム画面が表示された際にもランキングパネルを表示・更新
    /// </summary>
    [HarmonyPatch]
    public class MultiplayerPlusRoomViewActivatePatch
    {
        [HarmonyPrepare]
        static bool Prepare() => TargetMethod() != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.UI.MultiplayerPRoomMainView");
            if (type == null) return null;
            return AccessTools.Method(type, "OnViewActivation");
        }

        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                if (MultiplayerPlusBridge.IsInMultiplayerPlusRoom() &&
                    (MultiplayManager.Instance == null || !MultiplayManager.Instance.IsInSong))
                {
                    ResultsUIHost.Instance.ShowFloatingScreen();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in MultiplayerPlusRoomViewActivatePatch: " + ex);
            }
        }
    }

    /// <summary>
    /// Multiplayer+ から退出した際にフローティング画面を非表示にする
    /// </summary>
    [HarmonyPatch]
    public class MultiplayerPlusExitPatch
    {
        [HarmonyPrepare]
        static bool Prepare() => TargetMethod() != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Managers.RoomManager");
            if (type == null) return null;
            return AccessTools.Method(type, "OnExitMultiplayer");
        }

        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                ResultsUIHost.Instance.HideFloatingScreen();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error in MultiplayerPlusExitPatch: " + ex);
            }
        }
    }
}
