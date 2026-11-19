using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using HMUI;
using HarmonyLib;
using MoreFunMultiplay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoreFunMultiplay;

public class ResultsUIHost
{
	private FloatingScreen _floatingScreen;

	private bool _isCollapsed;

	private int _currentTab;

	private readonly string[] _tabContents = new string[7] { "<color=#FFD700>\ud83c\udfc6 本日の総合ランキング \ud83c\udfc6</color>\nまだ記録がありません", "<color=#00E5FF>\ud83c\udf10 通算の総合ランキング \ud83c\udf10</color>\nまだ記録がありません", "<color=#FF4400>\ud83d\udd25 運動量ランキング \ud83d\udd25</color>\nまだ記録がありません", "<color=#00FFFF>\ud83d\uddff 地蔵スタイル賞 \ud83d\uddff</color>\nまだ記録がありません", "<color=#FF00FF>\ud83c\udfaf チキンレース \ud83c\udfaf</color>\nまだ記録がありません", "<color=#A855F7>⚖\ufe0f シンメトリー ⚖\ufe0f</color>\nまだ記録がありません", "<color=#FF3366>\ud83d\udca5 Wild \ud83d\udca5</color>\nまだ記録がありません" };

	private readonly string[] _lobbyTabContents = new string[7] { "<color=#FFD700>\ud83c\udfc6 本日の総合ランキング \ud83c\udfc6</color>\nまだ記録がありません", "<color=#00E5FF>\ud83c\udf10 通算の総合ランキング \ud83c\udf10</color>\nまだ記録がありません", "<color=#FF4400>\ud83d\udd25 運動量ランキング \ud83d\udd25</color>\nまだ記録がありません", "<color=#00FFFF>\ud83d\uddff 地蔵スタイル賞 \ud83d\uddff</color>\nまだ記録がありません", "<color=#FF00FF>\ud83c\udfaf チキンレース \ud83c\udfaf</color>\nまだ記録がありません", "<color=#A855F7>⚖\ufe0f シンメトリー ⚖\ufe0f</color>\nまだ記録がありません", "<color=#FF3366>\ud83d\udca5 Wild \ud83d\udca5</color>\nまだ記録がありません" };

	private readonly int[] _tabItemCounts = new int[7];

	private readonly Dictionary<string, int> _lastEarnedPoints = new Dictionary<string, int>();

	private List<PlayerScoreData> _lastCompletedScores = new List<PlayerScoreData>();

	private bool _hasProcessedCurrentSong = true;

	[UIComponent("top-bar")]
	private RectTransform _topBar;

	[UIComponent("header-text")]
	private TextMeshProUGUI _headerText;

	[UIComponent("main-body")]
	private RectTransform _mainBody;

	[UIComponent("tab-row-1")]
	private RectTransform _tabRow1;

	[UIComponent("tab-row-2")]
	private RectTransform _tabRow2;

	[UIComponent("result-scroll")]
	private ScrollView _resultScrollView;

	[UIComponent("result-text")]
	private TextMeshProUGUI _resultText;

	[UIComponent("collapse-btn")]
	private Button _collapseBtn;

	private static FieldInfo _contentRectField;

	private static FieldInfo _pageUpBtnField;

	private static FieldInfo _pageDownBtnField;

	private static FieldInfo _scrollIndicatorField;

	public static ResultsUIHost Instance { get; } = new ResultsUIHost();


	public bool IsFloatingScreenVisible
	{
		get
		{
			if ((UnityEngine.Object)(object)_floatingScreen != null)
			{
				return ((Component)(object)_floatingScreen).gameObject.activeSelf;
			}
			return false;
		}
	}

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
	public void OnTab0()
	{
		SwitchTab(0);
	}

	[UIAction("tab-1")]
	public void OnTab1()
	{
		SwitchTab(1);
	}

	[UIAction("tab-2")]
	public void OnTab2()
	{
		SwitchTab(2);
	}

	[UIAction("tab-3")]
	public void OnTab3()
	{
		SwitchTab(3);
	}

	[UIAction("tab-4")]
	public void OnTab4()
	{
		SwitchTab(4);
	}

	[UIAction("tab-5")]
	public void OnTab5()
	{
		SwitchTab(5);
	}

	[UIAction("tab-6")]
	public void OnTab6()
	{
		SwitchTab(6);
	}

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
		{
			return _tabContents[index];
		}
		return "";
	}

	public string GetLobbyTabContent(int index)
	{
		if (index >= 0 && index < _lobbyTabContents.Length)
		{
			return _lobbyTabContents[index];
		}
		return "";
	}

	public void SwitchTab(int tabIndex)
	{
		if (tabIndex >= 0 && tabIndex < _tabContents.Length)
		{
			_currentTab = tabIndex;
			RefreshDisplayedText();
		}
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
			bool flag = _currentTab >= 0 && _currentTab < _tabItemCounts.Length && _tabItemCounts[_currentTab] >= 20;
			Component component = _pageUpBtnField?.GetValue(_resultScrollView) as Component;
			Component component2 = _pageDownBtnField?.GetValue(_resultScrollView) as Component;
			Component component3 = _scrollIndicatorField?.GetValue(_resultScrollView) as Component;
			if (component != null)
			{
				component.gameObject.SetActive(flag);
			}
			if (component2 != null)
			{
				component2.gameObject.SetActive(flag);
			}
			if (component3 != null)
			{
				component3.gameObject.SetActive(flag);
			}
			RectTransform rectTransform = _contentRectField?.GetValue(_resultScrollView) as RectTransform;
			if (rectTransform != null)
			{
				VerticalLayoutGroup[] componentsInChildren = rectTransform.GetComponentsInChildren<VerticalLayoutGroup>(includeInactive: true);
				foreach (VerticalLayoutGroup obj in componentsInChildren)
				{
					obj.childAlignment = TextAnchor.UpperCenter;
					obj.childForceExpandHeight = false;
					obj.childControlHeight = true;
				}
				LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
				rectTransform.anchoredPosition = Vector2.zero;
			}
			if (flag)
			{
				_resultScrollView.enabled = true;
				_resultScrollView.UpdateContentSize();
				_resultScrollView.ScrollTo(0f, animated: false);
			}
			else
			{
				_resultScrollView.ScrollTo(0f, animated: false);
				_resultScrollView.enabled = false;
				if (rectTransform != null)
				{
					rectTransform.anchoredPosition = Vector2.zero;
				}
			}
		}
		SettingsUI.Instance.OnExternalTabSwitched(_currentTab);
	}

	private static void EnsureScrollViewFieldsCached()
	{
		if (!(_contentRectField != null))
		{
			Type typeFromHandle = typeof(ScrollView);
			_contentRectField = AccessTools.Field(typeFromHandle, "_contentRectTransform");
			_pageUpBtnField = AccessTools.Field(typeFromHandle, "_pageUpButton");
			_pageDownBtnField = AccessTools.Field(typeFromHandle, "_pageDownButton");
			_scrollIndicatorField = AccessTools.Field(typeFromHandle, "_verticalScrollIndicator");
		}
	}

	private void ApplyCollapseState()
	{
		if (_mainBody != null)
		{
			_mainBody.gameObject.SetActive(!_isCollapsed);
		}
		if (_collapseBtn != null)
		{
			TMP_Text componentInChildren = _collapseBtn.GetComponentInChildren<TMP_Text>();
			if (componentInChildren != null)
			{
				componentInChildren.text = (_isCollapsed ? "表示 ▼" : "非表示 ▲");
			}
		}
	}

	public static bool IsSongCompletedToEnd(LevelCompletionResults results)
	{
		if (results == null)
		{
			return false;
		}
		if (results.levelEndAction != 0)
		{
			return false;
		}
		if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared)
		{
			return true;
		}
		if (PluginConfig.Instance != null && PluginConfig.Instance.SelectedGameMode == 5 && results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed)
		{
			return true;
		}
		return false;
	}

	public void ProcessSongEndIfNeeded(LevelCompletionResults results)
	{
		if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
		{
			return;
		}
		MultiplayManager instance = MultiplayManager.Instance;
		if (instance == null)
		{
			return;
		}
		if (!_hasProcessedCurrentSong)
		{
			_hasProcessedCurrentSong = true;
			bool num = MultiplayerPlusBridge.IsLocalPlayerRoomHost();
			if (results != null)
			{
				instance.LocalPlayerMissCount = results.missedCount + results.badCutsCount;
				instance.LocalPlayerHitCount = results.goodCutsCount;
				instance.LocalPlayerFailed = results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed || results.energy <= 0.0001f;
			}
			if (num)
			{
				if (IsSongCompletedToEnd(results))
				{
					instance.CompleteSong();
					_lastCompletedScores = CollectLiveScores();
					CalculateAndAwardPoints();
					int currentTab = 0;
					switch ((GameMode)PluginConfig.Instance.SelectedGameMode)
					{
					case GameMode.Sweat:
						currentTab = 2;
						break;
					case GameMode.Jizo:
						currentTab = 3;
						break;
					case GameMode.ChickenRace:
						currentTab = 4;
						break;
					case GameMode.Symmetry:
						currentTab = 5;
						break;
					case GameMode.Wild:
						currentTab = 6;
						break;
					}
					_currentTab = currentTab;
					UpdateTabContents();
					RefreshDisplayedText();
					PublishCurrentStateToCloud(isSongEndUpdate: true);
					return;
				}
				instance.AbortSong();
				Plugin.Log.Info($"Song ended midway (State={results?.levelEndStateType}, Action={results?.levelEndAction}). Points not awarded.");
			}
			else
			{
				instance.CompleteSong();
				_lastCompletedScores = CollectLiveScores();
				RoomSyncManager.TriggerParticipantFetchWithRetries(MultiplayerPlusBridge.GetCurrentRoomCode());
			}
		}
		else if (instance.IsInSong)
		{
			instance.StopSong();
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
		if (!(_headerText == null))
		{
			string currentRoomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
			if (!MultiplayerPlusBridge.IsLocalPlayerRoomHost())
			{
				string roomHostName = MultiplayerPlusBridge.GetRoomHostName();
				_headerText.text = (string.IsNullOrEmpty(currentRoomCode) ? ("\ud83c\udfc6 MFM (Host: " + roomHostName + ")") : ("\ud83c\udfc6 MFM [" + currentRoomCode + "]"));
			}
			else
			{
				_headerText.text = (string.IsNullOrEmpty(currentRoomCode) ? "\ud83c\udfc6 MoreFunMultiplay" : ("\ud83c\udfc6 MFM [Code: " + currentRoomCode + "]"));
			}
		}
	}

	public void PublishCurrentStateToCloud(bool isSongEndUpdate)
	{
		if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom() || !MultiplayerPlusBridge.IsLocalPlayerRoomHost())
		{
			return;
		}
		string currentRoomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
		if (!string.IsNullOrEmpty(currentRoomCode))
		{
			RoomRankingSnapshot roomRankingSnapshot = new RoomRankingSnapshot
			{
				Version = 1,
				RoomCode = currentRoomCode,
				HostName = MultiplayerPlusBridge.GetLocalPlayerName(),
				SelectedGameMode = ((PluginConfig.Instance != null) ? PluginConfig.Instance.SelectedGameMode : 0),
				TargetScorePercentage = ((PluginConfig.Instance != null) ? PluginConfig.Instance.TargetScorePercentage : 90f),
				RecommendedTab = _currentTab,
				DailyMatchesPlayed = SessionStore.Current.DailyMatchesPlayed,
				TotalMatchesPlayed = SessionStore.Current.TotalMatchesPlayed,
				IsSongEndUpdate = isSongEndUpdate
			};
			for (int i = 0; i < 7; i++)
			{
				roomRankingSnapshot.TabContents[i] = _tabContents[i] ?? "";
				roomRankingSnapshot.LobbyTabContents[i] = _lobbyTabContents[i] ?? "";
				roomRankingSnapshot.TabItemCounts[i] = _tabItemCounts[i];
			}
			RoomSyncManager.PublishHostSnapshot(roomRankingSnapshot, isSongEndUpdate);
		}
	}

	public void ApplyRemoteSnapshot(RoomRankingSnapshot snapshot)
	{
		if (snapshot == null || !MultiplayerPlusBridge.IsInMultiplayerPlusRoom() || MultiplayerPlusBridge.IsLocalPlayerRoomHost())
		{
			return;
		}
		if (snapshot.TabContents != null)
		{
			int num = Math.Min(snapshot.TabContents.Length, _tabContents.Length);
			for (int i = 0; i < num; i++)
			{
				if (!string.IsNullOrEmpty(snapshot.TabContents[i]))
				{
					_tabContents[i] = snapshot.TabContents[i];
				}
			}
		}
		if (snapshot.LobbyTabContents != null)
		{
			int num2 = Math.Min(snapshot.LobbyTabContents.Length, _lobbyTabContents.Length);
			for (int j = 0; j < num2; j++)
			{
				if (!string.IsNullOrEmpty(snapshot.LobbyTabContents[j]))
				{
					_lobbyTabContents[j] = snapshot.LobbyTabContents[j];
				}
			}
		}
		if (snapshot.TabItemCounts != null)
		{
			int num3 = Math.Min(snapshot.TabItemCounts.Length, _tabItemCounts.Length);
			for (int k = 0; k < num3; k++)
			{
				_tabItemCounts[k] = snapshot.TabItemCounts[k];
			}
		}
		if (snapshot.IsSongEndUpdate && snapshot.RecommendedTab >= 0 && snapshot.RecommendedTab < _tabContents.Length)
		{
			_currentTab = snapshot.RecommendedTab;
		}
		UpdateHeaderText();
		RefreshDisplayedText();
	}

	public static int GetRankPoints(int rank, int totalPlayers)
	{
		if (totalPlayers <= 0 || rank <= 0 || rank > totalPlayers)
		{
			return 0;
		}
		int num = totalPlayers - rank + 1;
		int num2 = ((rank == 1) ? 2 : ((rank == 2) ? 1 : 0));
		return num + num2;
	}

	private float GetPlayerAccuracy(string playerName, List<PlayerScoreData> scores)
	{
		if (scores != null && !string.IsNullOrEmpty(playerName))
		{
			int num = scores.FindIndex((PlayerScoreData s) => string.Equals(s.Name, playerName, StringComparison.OrdinalIgnoreCase) || s.Name == playerName);
			if (num >= 0)
			{
				return Mathf.Clamp(scores[num].Percent, 0f, 100f);
			}
		}
		return 0f;
	}

	private void CalculateAndAwardPoints()
	{
		if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
		{
			return;
		}
		MultiplayManager instance = MultiplayManager.Instance;
		if (instance == null)
		{
			return;
		}
		Dictionary<string, MultiplayManager.PlayerMotionData> trackedPlayers = instance.GetTrackedPlayers();
		List<PlayerScoreData> scores = ExtractAllScores();
		GameMode selectedGameMode = (GameMode)PluginConfig.Instance.SelectedGameMode;
		_lastEarnedPoints.Clear();
		List<(string, float)> list = null;
		if (selectedGameMode == GameMode.Sweat && trackedPlayers != null && trackedPlayers.Count > 0)
		{
			list = (from x in trackedPlayers.Values.Select(delegate(MultiplayManager.PlayerMotionData p)
				{
					float playerAccuracy3 = GetPlayerAccuracy(p.PlayerName, scores);
					float item5 = p.TotalDistance * (playerAccuracy3 / 100f);
					return (Name: p.PlayerName, Eval: item5, Acc: playerAccuracy3, Dist: p.TotalDistance);
				})
				orderby x.Eval descending, x.Acc descending, x.Dist descending
				select (Name: x.Name, Eval: x.Eval)).ToList();
		}
		else if (selectedGameMode == GameMode.Jizo && trackedPlayers != null && trackedPlayers.Count > 0)
		{
			list = (from x in trackedPlayers.Values.Select(delegate(MultiplayManager.PlayerMotionData p)
				{
					float playerAccuracy2 = GetPlayerAccuracy(p.PlayerName, scores);
					float item4 = ((playerAccuracy2 > 0.001f) ? (p.HeadDistance / (playerAccuracy2 / 100f)) : float.MaxValue);
					return (Name: p.PlayerName, Eval: item4, Acc: playerAccuracy2, HeadDist: p.HeadDistance);
				})
				orderby x.Eval, x.Acc descending, x.HeadDist
				select (Name: x.Name, Eval: x.Eval)).ToList();
		}
		else if (selectedGameMode == GameMode.ChickenRace && scores != null && scores.Count > 0)
		{
			float target = PluginConfig.Instance.TargetScorePercentage;
			list = (from s in scores
				orderby Math.Abs(s.Percent - target)
				select (Name: s.Name, Percent: s.Percent)).ToList();
		}
		else if (selectedGameMode == GameMode.Symmetry && trackedPlayers != null && trackedPlayers.Count > 0)
		{
			list = (from x in trackedPlayers.Values.Select(delegate(MultiplayManager.PlayerMotionData p)
				{
					float playerAccuracy = GetPlayerAccuracy(p.PlayerName, scores);
					float num = p.LeftHandDistance + p.RightHandDistance;
					float num2 = ((num > 0.0001f) ? (Mathf.Abs(p.RightHandDistance - p.LeftHandDistance) / num * 100f) : 0f);
					float item3 = playerAccuracy / (1f + num2);
					return (Name: p.PlayerName, Eval: item3, Acc: playerAccuracy, DiffRate: num2);
				})
				orderby x.Eval descending, x.Acc descending, x.DiffRate
				select (Name: x.Name, Eval: x.Eval)).ToList();
		}
		else if (selectedGameMode == GameMode.Wild && scores != null && scores.Count > 0)
		{
			list = (from x in scores.Select(delegate(PlayerScoreData s)
				{
					bool flag = !s.Failed && (s.Percent > 0.001f || s.HitCount > 0);
					float item2 = (flag ? ((float)s.MissCount + (100f - s.Percent) / 100f) : s.Percent);
					return (Name: s.Name, IsCleared: flag, MissCount: s.MissCount, Acc: s.Percent, Eval: item2);
				})
				orderby x.IsCleared ? 1 : 0 descending, x.IsCleared ? x.MissCount : 0 descending, (!x.IsCleared) ? 0f : x.Acc, x.IsCleared ? 0f : x.Acc descending, (!x.IsCleared) ? x.MissCount : 0
				select (Name: x.Name, Eval: x.Eval)).ToList();
		}
		if (list != null && list.Count > 0)
		{
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				int rankPoints = GetRankPoints(i + 1, count);
				string item = list[i].Item1;
				_lastEarnedPoints[item] = rankPoints;
				SessionStore.AddPoints(item, rankPoints);
			}
			SessionStore.Current.DailyMatchesPlayed++;
			SessionStore.Current.TotalMatchesPlayed++;
			SessionStore.Save();
			Plugin.Log.Info($"Awarded points to {count} players in Multiplayer+. DailyMatches: {SessionStore.Current.DailyMatchesPlayed}, TotalMatches: {SessionStore.Current.TotalMatchesPlayed}");
		}
	}

	public void UpdateTabContents()
	{
		MultiplayManager instance = MultiplayManager.Instance;
		Dictionary<string, MultiplayManager.PlayerMotionData> dictionary = ((instance != null) ? instance.GetTrackedPlayers() : null);
		List<PlayerScoreData> scores = ExtractAllScores();
		GameMode selectedGameMode = (GameMode)PluginConfig.Instance.SelectedGameMode;
		string text = $"<color=#FFD700>\ud83c\udfc6 本日の総合 (本日: {SessionStore.Current.DailyMatchesPlayed}曲)</color>\n";
		string text2 = "";
		string text3 = "";
		_tabItemCounts[0] = SessionStore.Current.DailyScores.Count;
		if (SessionStore.Current.DailyScores.Count > 0)
		{
			List<KeyValuePair<string, int>> list = SessionStore.Current.DailyScores.OrderByDescending((KeyValuePair<string, int> s) => s.Value).ToList();
			for (int i = 0; i < list.Count; i++)
			{
				string text4 = i switch
				{
					2 => "<color=#CD7F32>3位</color>", 
					1 => "<color=#E0E0E0>2位</color>", 
					0 => "<color=#FFD700>1位</color>", 
					_ => $"{i + 1}位", 
				};
				string key = list[i].Key;
				int value;
				string text5 = ((_lastEarnedPoints.TryGetValue(key, out value) && value > 0 && SessionStore.Current.DailyMatchesPlayed > 0) ? $" <color=#FFD700>(+{value}pt)</color>" : "");
				string text6 = $"{text4} {key}: <color=#00FF88>{list[i].Value} pt</color>{text5}\n";
				text2 += text6;
				if (i < 5)
				{
					text3 += text6;
				}
			}
		}
		else
		{
			text2 = "本日の得点記録はまだありません\n";
			text3 = "本日の得点記録はまだありません\n";
		}
		_tabContents[0] = text + text2;
		_lobbyTabContents[0] = text + text3;
		string text7 = $"<color=#00E5FF>\ud83c\udf10 通算の総合 (通算: {SessionStore.Current.TotalMatchesPlayed}曲)</color>\n";
		string text8 = "";
		string text9 = "";
		_tabItemCounts[1] = SessionStore.Current.TotalScores.Count;
		if (SessionStore.Current.TotalScores.Count > 0)
		{
			List<KeyValuePair<string, int>> list2 = SessionStore.Current.TotalScores.OrderByDescending((KeyValuePair<string, int> s) => s.Value).ToList();
			for (int j = 0; j < list2.Count; j++)
			{
				string text10 = j switch
				{
					2 => "<color=#CD7F32>3位</color>", 
					1 => "<color=#E0E0E0>2位</color>", 
					0 => "<color=#FFD700>1位</color>", 
					_ => $"{j + 1}位", 
				};
				string key2 = list2[j].Key;
				int value2;
				string text11 = ((_lastEarnedPoints.TryGetValue(key2, out value2) && value2 > 0 && SessionStore.Current.TotalMatchesPlayed > 0) ? $" <color=#FFD700>(+{value2}pt)</color>" : "");
				string text12 = $"{text10} {key2}: <color=#00FF88>{list2[j].Value} pt</color>{text11}\n";
				text8 += text12;
				if (j < 5)
				{
					text9 += text12;
				}
			}
		}
		else
		{
			text8 = "通算の得点記録はまだありません\n";
			text9 = "通算の得点記録はまだありません\n";
		}
		_tabContents[1] = text7 + text8;
		_lobbyTabContents[1] = text7 + text9;
		string text13 = "<color=#FF4400>\ud83d\udd25 運動量 (移動×精度%)</color>";
		if (selectedGameMode == GameMode.Sweat)
		{
			text13 += " <color=#FFD700>[★勝負]</color>";
		}
		text13 += "\n";
		string text14 = "";
		string text15 = "";
		_tabItemCounts[2] = dictionary?.Count ?? 0;
		if (dictionary != null && dictionary.Count > 0)
		{
			List<(string, float, float, float)> list3 = (from x in dictionary.Values.Select(delegate(MultiplayManager.PlayerMotionData p)
				{
					float playerAccuracy3 = GetPlayerAccuracy(p.PlayerName, scores);
					float item4 = p.TotalDistance * (playerAccuracy3 / 100f);
					return (Name: p.PlayerName, Dist: p.TotalDistance, Acc: playerAccuracy3, Eval: item4);
				})
				orderby x.Eval descending, x.Acc descending, x.Dist descending
				select x).ToList();
			int count = list3.Count;
			for (int k = 0; k < count; k++)
			{
				int num = k + 1;
				int rankPoints = GetRankPoints(num, count);
				string text16 = k switch
				{
					2 => "<color=#CD7F32>3位</color>", 
					1 => "<color=#E0E0E0>2位</color>", 
					0 => "<color=#FFD700>1位</color>", 
					_ => $"{num}位", 
				};
				string text17 = $"{text16} {list3[k].Item1}: <color=#FFCC00>{list3[k].Item4:F1}</color> ({list3[k].Item2:F1}m×{list3[k].Item3:F1}%) <color=#00FF88>+{rankPoints}pt</color>\n";
				text14 += text17;
				if (k < 5)
				{
					text15 += text17;
				}
			}
		}
		else
		{
			text14 = "直近の曲データなし\n";
			text15 = "直近の曲データなし\n";
		}
		_tabContents[2] = text13 + text14;
		_lobbyTabContents[2] = text13 + text15;
		string text18 = "<color=#00FFFF>\ud83d\uddff 地蔵 (頭移動÷精度%・小順)</color>";
		if (selectedGameMode == GameMode.Jizo)
		{
			text18 += " <color=#FFD700>[★勝負]</color>";
		}
		text18 += "\n";
		string text19 = "";
		string text20 = "";
		_tabItemCounts[3] = dictionary?.Count ?? 0;
		if (dictionary != null && dictionary.Count > 0)
		{
			List<(string, float, float, bool, float)> list4 = (from x in dictionary.Values.Select(delegate(MultiplayManager.PlayerMotionData p)
				{
					float playerAccuracy2 = GetPlayerAccuracy(p.PlayerName, scores);
					bool flag2 = playerAccuracy2 > 0.001f;
					float item3 = (flag2 ? (p.HeadDistance / (playerAccuracy2 / 100f)) : float.MaxValue);
					return (Name: p.PlayerName, HeadDist: p.HeadDistance, Acc: playerAccuracy2, ValidAcc: flag2, Eval: item3);
				})
				orderby x.Eval, x.Acc descending, x.HeadDist
				select x).ToList();
			int count2 = list4.Count;
			for (int l = 0; l < count2; l++)
			{
				int num2 = l + 1;
				int rankPoints2 = GetRankPoints(num2, count2);
				string text21 = l switch
				{
					2 => "<color=#CD7F32>3位</color>", 
					1 => "<color=#E0E0E0>2位</color>", 
					0 => "<color=#FFD700>1位</color>", 
					_ => $"{num2}位", 
				};
				string text22 = (list4[l].Item4 ? $"<color=#00FFFF>{list4[l].Item5:F1}</color>" : "<color=#888888>記録なし</color>");
				string text23 = $"{text21} {list4[l].Item1}: {text22} ({list4[l].Item2:F1}m÷{list4[l].Item3:F1}%) <color=#00FF88>+{rankPoints2}pt</color>\n";
				text19 += text23;
				if (l < 5)
				{
					text20 += text23;
				}
			}
		}
		else
		{
			text19 = "直近の曲データなし\n";
			text20 = "直近の曲データなし\n";
		}
		_tabContents[3] = text18 + text19;
		_lobbyTabContents[3] = text18 + text20;
		float target = PluginConfig.Instance.TargetScorePercentage;
		string text24 = $"<color=#FF00FF>\ud83c\udfaf チキンレース (目標: {target:F0}%)</color>";
		if (selectedGameMode == GameMode.ChickenRace)
		{
			text24 += " <color=#FFD700>[★勝負]</color>";
		}
		text24 += "\n";
		string text25 = "";
		string text26 = "";
		_tabItemCounts[4] = ((scores != null) ? scores.Count : 0);
		if (scores != null && scores.Count > 0)
		{
			List<PlayerScoreData> list5 = scores.OrderBy((PlayerScoreData s) => Math.Abs(s.Percent - target)).ToList();
			int count3 = list5.Count;
			for (int m = 0; m < count3; m++)
			{
				int num3 = m + 1;
				int rankPoints3 = GetRankPoints(num3, count3);
				float num4 = Math.Abs(list5[m].Percent - target);
				string text27 = m switch
				{
					2 => "<color=#CD7F32>3位</color>", 
					1 => "<color=#E0E0E0>2位</color>", 
					0 => "<color=#FFD700>1位</color>", 
					_ => $"{num3}位", 
				};
				string text28 = $"{text27} {list5[m].Name}: {list5[m].Percent:F2}% (差 {num4:F2}%) <color=#00FF88>+{rankPoints3}pt</color>\n";
				text25 += text28;
				if (m < 5)
				{
					text26 += text28;
				}
			}
		}
		else
		{
			text25 = "直近のスコアデータなし\n";
			text26 = "直近のスコアデータなし\n";
		}
		_tabContents[4] = text24 + text25;
		_lobbyTabContents[4] = text24 + text26;
		string text29 = "<color=#A855F7>⚖\ufe0f シンメトリー (左右均等×精度%)</color>";
		if (selectedGameMode == GameMode.Symmetry)
		{
			text29 += " <color=#FFD700>[★勝負]</color>";
		}
		text29 += "\n";
		string text30 = "";
		string text31 = "";
		_tabItemCounts[5] = dictionary?.Count ?? 0;
		if (dictionary != null && dictionary.Count > 0)
		{
			List<(string, float, float, float, float, float)> list6 = (from x in dictionary.Values.Select(delegate(MultiplayManager.PlayerMotionData p)
				{
					float playerAccuracy = GetPlayerAccuracy(p.PlayerName, scores);
					float num8 = p.LeftHandDistance + p.RightHandDistance;
					float num9 = ((num8 > 0.0001f) ? (Mathf.Abs(p.RightHandDistance - p.LeftHandDistance) / num8 * 100f) : 0f);
					float item2 = playerAccuracy / (1f + num9);
					return (Name: p.PlayerName, Left: p.LeftHandDistance, Right: p.RightHandDistance, DiffRate: num9, Acc: playerAccuracy, Eval: item2);
				})
				orderby x.Eval descending, x.Acc descending, x.DiffRate
				select x).ToList();
			int count4 = list6.Count;
			for (int n = 0; n < count4; n++)
			{
				int num5 = n + 1;
				int rankPoints4 = GetRankPoints(num5, count4);
				string text32 = n switch
				{
					2 => "<color=#CD7F32>3位</color>", 
					1 => "<color=#E0E0E0>2位</color>", 
					0 => "<color=#FFD700>1位</color>", 
					_ => $"{num5}位", 
				};
				string text33 = $"{text32} {list6[n].Item1}: <color=#D8B4FE>{list6[n].Item6:F2}</color> (L:{list6[n].Item2:F1}m/R:{list6[n].Item3:F1}m 差{list6[n].Item4:F1}%×{list6[n].Item5:F1}%) <color=#00FF88>+{rankPoints4}pt</color>\n";
				text30 += text33;
				if (n < 5)
				{
					text31 += text33;
				}
			}
		}
		else
		{
			text30 = "直近の曲データなし\n";
			text31 = "直近の曲データなし\n";
		}
		_tabContents[5] = text29 + text30;
		_lobbyTabContents[5] = text29 + text31;
		string text34 = "<color=#FF3366>\ud83d\udca5 Wild (クリア最多ミス・Fail時高精度)</color>";
		if (selectedGameMode == GameMode.Wild)
		{
			text34 += " <color=#FFD700>[★勝負]</color>";
		}
		text34 += "\n";
		string text35 = "";
		string text36 = "";
		_tabItemCounts[6] = ((scores != null) ? scores.Count : 0);
		if (scores != null && scores.Count > 0)
		{
			List<(string, bool, int, float, float)> list7 = (from x in scores.Select(delegate(PlayerScoreData s)
				{
					bool flag = !s.Failed && (s.Percent > 0.001f || s.HitCount > 0);
					float item = (flag ? ((float)s.MissCount + (100f - s.Percent) / 100f) : s.Percent);
					return (Name: s.Name, IsCleared: flag, MissCount: s.MissCount, Acc: s.Percent, Eval: item);
				})
				orderby x.IsCleared ? 1 : 0 descending, x.IsCleared ? x.MissCount : 0 descending, (!x.IsCleared) ? 0f : x.Acc, x.IsCleared ? 0f : x.Acc descending, (!x.IsCleared) ? x.MissCount : 0
				select x).ToList();
			int count5 = list7.Count;
			for (int num6 = 0; num6 < count5; num6++)
			{
				int num7 = num6 + 1;
				int rankPoints5 = GetRankPoints(num7, count5);
				string text37 = num6 switch
				{
					2 => "<color=#CD7F32>3位</color>", 
					1 => "<color=#E0E0E0>2位</color>", 
					0 => "<color=#FFD700>1位</color>", 
					_ => $"{num7}位", 
				};
				string text38 = ((!list7[num6].Item2) ? $"{text37} {list7[num6].Item1}: <color=#888888>\ud83d\udc80 FAILED</color> ({list7[num6].Item4:F1}% / {list7[num6].Item3} Miss) <color=#00FF88>+{rankPoints5}pt</color>\n" : $"{text37} {list7[num6].Item1}: <color=#FF5555>{list7[num6].Item3} Miss</color> ({list7[num6].Item4:F1}% / 評価{list7[num6].Item5:F2}) <color=#00FF88>+{rankPoints5}pt</color>\n");
				text35 += text38;
				if (num6 < 5)
				{
					text36 += text38;
				}
			}
		}
		else
		{
			text35 = "直近のスコアデータなし\n";
			text36 = "直近のスコアデータなし\n";
		}
		_tabContents[6] = text34 + text35;
		_lobbyTabContents[6] = text34 + text36;
		RefreshDisplayedText();
	}

	private List<PlayerScoreData> CollectLiveScores()
	{
		List<PlayerScoreData> list = new List<PlayerScoreData>();
		if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
		{
			return list;
		}
		MultiplayerPlusBridge.CollectRoomScores(list);
		if (MultiplayManager.Instance != null)
		{
			MultiplayManager instance = MultiplayManager.Instance;
			float num = ((instance.LastCompletedLocalAccuracyPercent >= 0f) ? instance.LastCompletedLocalAccuracyPercent : instance.LocalPlayerAccuracyPercent);
			int num2 = Math.Max(instance.LastCompletedLocalMissCount, instance.LocalPlayerMissCount);
			int num3 = Math.Max(instance.LastCompletedLocalHitCount, instance.LocalPlayerHitCount);
			bool flag = instance.LastCompletedLocalFailed || instance.LocalPlayerFailed;
			if (num >= 0f)
			{
				string localName = instance.LocalPlayerName ?? MultiplayerPlusBridge.GetLocalPlayerName();
				int num4 = list.FindIndex((PlayerScoreData x) => x.Name == localName);
				if (num4 < 0)
				{
					list.Add(new PlayerScoreData
					{
						Name = localName,
						Percent = num,
						MissCount = num2,
						HitCount = num3,
						Failed = flag
					});
				}
				else
				{
					PlayerScoreData value = list[num4];
					if (value.Percent <= 0f && num > 0f)
					{
						value.Percent = num;
					}
					if (num2 > value.MissCount)
					{
						value.MissCount = num2;
					}
					if (num3 > value.HitCount)
					{
						value.HitCount = num3;
					}
					if (flag)
					{
						value.Failed = true;
					}
					list[num4] = value;
				}
			}
		}
		return list;
	}

	private List<PlayerScoreData> ExtractAllScores()
	{
		if (_lastCompletedScores != null && _lastCompletedScores.Count > 0)
		{
			return _lastCompletedScores;
		}
		return CollectLiveScores();
	}

	private void EnsureFloatingScreenCreated()
	{
		if (!((UnityEngine.Object)(object)_floatingScreen != null))
		{
			PluginConfig instance = PluginConfig.Instance;
			Vector3 position = ((instance != null) ? new Vector3(instance.FloatingPosX, instance.FloatingPosY, instance.FloatingPosZ) : new Vector3(0f, 2.25f, 2.5f));
			Quaternion rotation = ((instance != null) ? Quaternion.Euler(instance.FloatingRotX, instance.FloatingRotY, instance.FloatingRotZ) : Quaternion.Euler(-12f, 0f, 0f));
			_isCollapsed = instance?.IsResultsCollapsed ?? false;
			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(72f, 86f), createHandle: true, position, rotation, 0f, hasBackground: false);
			_floatingScreen.ShowHandle = true;
			_floatingScreen.HighlightHandle = true;
			_floatingScreen.HandleSide = FloatingScreen.Side.Top;
			((Component)(object)_floatingScreen).gameObject.name = "MoreFunMultiplayFloatingScreen";
			UnityEngine.Object.DontDestroyOnLoad(((Component)(object)_floatingScreen).gameObject);
			GameObject gameObject = AccessTools.Field(typeof(FloatingScreen), "handle")?.GetValue(_floatingScreen) as GameObject;
			if (gameObject != null)
			{
				gameObject.transform.localPosition = new Vector3(0f, 43.5f, 0f);
				gameObject.transform.localScale = new Vector3(64f, 4f, 4f);
			}
			_floatingScreen.HandleReleased -= OnFloatingScreenHandleReleased;
			_floatingScreen.HandleReleased += OnFloatingScreenHandleReleased;
			BSMLCompat.Parse("<bg xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='https://monkeymanboy.github.io/BSML-Docs/ https://raw.githubusercontent.com/monkeymanboy/BSML-Docs/gh-pages/BSMLSchema.xsd'>\n  <horizontal id='top-bar' bg='round-rect-panel' pad='1' spacing='2' pref-width='70' pref-height='8.5' child-align='MiddleCenter'>\n    <text id='header-text' text='\ud83c\udfc6 MoreFunMultiplay' font-size='2.8' align='Center' rich-text='true'/>\n    <button id='collapse-btn' text='非表示 ▲' on-click='toggle-collapse' pref-width='22' pref-height='6.8' font-size='2.8'/>\n  </horizontal>\n  <vertical id='main-body' bg='round-rect-panel' pad='1' spacing='1' pref-width='70' pref-height='73'>\n    <horizontal id='tab-row-1' spacing='2' horizontal-fit='PreferredSize' child-align='MiddleCenter'>\n      <button text='\ud83c\udfc6 当日総合' on-click='tab-0' pref-width='31' pref-height='7.5' font-size='3.0'/>\n      <button text='\ud83c\udf10 通算総合' on-click='tab-1' pref-width='31' pref-height='7.5' font-size='3.0'/>\n    </horizontal>\n    <horizontal id='tab-row-2' spacing='0.8' horizontal-fit='PreferredSize' child-align='MiddleCenter'>\n      <button text='\ud83d\udd25運動量' on-click='tab-2' pref-width='12.5' pref-height='7.5' font-size='2.45'/>\n      <button text='\ud83d\uddff地蔵' on-click='tab-3' pref-width='12.0' pref-height='7.5' font-size='2.45'/>\n      <button text='\ud83c\udfafチキン' on-click='tab-4' pref-width='12.5' pref-height='7.5' font-size='2.45'/>\n      <button text='⚖\ufe0fシンメ' on-click='tab-5' pref-width='12.5' pref-height='7.5' font-size='2.45'/>\n      <button text='\ud83d\udca5Wild' on-click='tab-6' pref-width='12.5' pref-height='7.5' font-size='2.45'/>\n    </horizontal>\n    <scroll-view id='result-scroll' pref-width='66' pref-height='53'>\n      <text id='result-text' text='読み込み中...' align='Top' font-size='2.45' rich-text='true'/>\n    </scroll-view>\n  </vertical>\n</bg>", ((Component)(object)_floatingScreen).gameObject, this);
			FixupFloatingScreenLayout();
			ApplyCollapseState();
			UpdateHeaderText();
			if (MultiplayerPlusBridge.IsLocalPlayerRoomHost())
			{
				UpdateTabContents();
			}
		}
	}

	private void FixupFloatingScreenLayout()
	{
		if (_topBar != null)
		{
			ContentSizeFitter component = _topBar.GetComponent<ContentSizeFitter>();
			if (component != null)
			{
				component.enabled = false;
			}
			_topBar.anchorMin = new Vector2(0.5f, 0.5f);
			_topBar.anchorMax = new Vector2(0.5f, 0.5f);
			_topBar.pivot = new Vector2(0.5f, 0.5f);
			_topBar.anchoredPosition = new Vector2(0f, 36.5f);
			_topBar.sizeDelta = new Vector2(70f, 8.5f);
		}
		if (_mainBody != null)
		{
			VerticalLayoutGroup component2 = _mainBody.GetComponent<VerticalLayoutGroup>();
			if (component2 != null)
			{
				component2.enabled = false;
			}
			ContentSizeFitter component3 = _mainBody.GetComponent<ContentSizeFitter>();
			if (component3 != null)
			{
				component3.enabled = false;
			}
			_mainBody.anchorMin = new Vector2(0.5f, 0.5f);
			_mainBody.anchorMax = new Vector2(0.5f, 0.5f);
			_mainBody.pivot = new Vector2(0.5f, 0.5f);
			_mainBody.anchoredPosition = new Vector2(0f, -5.5f);
			_mainBody.sizeDelta = new Vector2(70f, 73f);
		}
		if (_tabRow1 != null)
		{
			ContentSizeFitter component4 = _tabRow1.GetComponent<ContentSizeFitter>();
			if (component4 != null)
			{
				component4.enabled = false;
			}
			_tabRow1.anchorMin = new Vector2(0.5f, 0.5f);
			_tabRow1.anchorMax = new Vector2(0.5f, 0.5f);
			_tabRow1.pivot = new Vector2(0.5f, 0.5f);
			_tabRow1.anchoredPosition = new Vector2(0f, 30.5f);
			_tabRow1.sizeDelta = new Vector2(66f, 8f);
		}
		if (_tabRow2 != null)
		{
			ContentSizeFitter component5 = _tabRow2.GetComponent<ContentSizeFitter>();
			if (component5 != null)
			{
				component5.enabled = false;
			}
			_tabRow2.anchorMin = new Vector2(0.5f, 0.5f);
			_tabRow2.anchorMax = new Vector2(0.5f, 0.5f);
			_tabRow2.pivot = new Vector2(0.5f, 0.5f);
			_tabRow2.anchoredPosition = new Vector2(0f, 22f);
			_tabRow2.sizeDelta = new Vector2(66f, 8f);
		}
		if (_resultScrollView != null)
		{
			RectTransform rectTransform = _resultScrollView.transform as RectTransform;
			if (rectTransform != null)
			{
				rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
				rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
				rectTransform.pivot = new Vector2(0.5f, 0.5f);
				rectTransform.anchoredPosition = new Vector2(0f, -9f);
				rectTransform.sizeDelta = new Vector2(66f, 53f);
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
			if (PluginConfig.Instance != null && !((UnityEngine.Object)(object)_floatingScreen == null))
			{
				Vector3 screenPosition = _floatingScreen.ScreenPosition;
				Vector3 eulerAngles = _floatingScreen.ScreenRotation.eulerAngles;
				PluginConfig.Instance.FloatingPosX = screenPosition.x;
				PluginConfig.Instance.FloatingPosY = screenPosition.y;
				PluginConfig.Instance.FloatingPosZ = screenPosition.z;
				PluginConfig.Instance.FloatingRotX = eulerAngles.x;
				PluginConfig.Instance.FloatingRotY = eulerAngles.y;
				PluginConfig.Instance.FloatingRotZ = eulerAngles.z;
			}
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
		if (!((UnityEngine.Object)(object)_floatingScreen != null))
		{
			return;
		}
		((Component)(object)_floatingScreen).gameObject.SetActive(value: true);
		FixupFloatingScreenLayout();
		ApplyCollapseState();
		UpdateHeaderText();
		if (MultiplayerPlusBridge.IsLocalPlayerRoomHost())
		{
			UpdateTabContents();
		}
		else
		{
			string currentRoomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
			if (!string.IsNullOrEmpty(currentRoomCode))
			{
				RoomSyncManager.PollLatestRoomSnapshotAsync(currentRoomCode);
			}
		}
		RefreshDisplayedText();
	}

	public void HideFloatingScreen()
	{
		if ((UnityEngine.Object)(object)_floatingScreen != null)
		{
			((Component)(object)_floatingScreen).gameObject.SetActive(value: false);
		}
	}
}
