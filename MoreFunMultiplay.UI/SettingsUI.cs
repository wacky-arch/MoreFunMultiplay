using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using TMPro;
using UnityEngine;

namespace MoreFunMultiplay.UI;

public class SettingsUI
{
	private bool _isInitialized;

	private bool _pendingTotalReset;

	[UIComponent("lobby-ranking-text")]
	private TextMeshProUGUI _lobbyRankingText;

	[UIComponent("gp-reset-normal-row")]
	private RectTransform _gpResetNormalRow;

	[UIComponent("gp-reset-confirm-row")]
	private RectTransform _gpResetConfirmRow;

	[UIComponent("gp-reset-confirm-text")]
	private TextMeshProUGUI _gpResetConfirmText;

	[UIComponent("st-reset-normal-row")]
	private RectTransform _stResetNormalRow;

	[UIComponent("st-reset-confirm-row")]
	private RectTransform _stResetConfirmRow;

	[UIComponent("st-reset-confirm-text")]
	private TextMeshProUGUI _stResetConfirmText;

	[UIValue("game-mode-options")]
	public List<object> GameModeOptions = new List<object> { "勝負なし (通常)", "運動量ランキング", "地蔵スタイル賞", "チキンレース", "シンメトリー", "Wild" };

	public static SettingsUI Instance { get; } = new SettingsUI();


	[UIValue("game-mode")]
	public string CurrentGameModeStr
	{
		get
		{
			return (GameMode)PluginConfig.Instance.SelectedGameMode switch
			{
				GameMode.Sweat => "運動量ランキング", 
				GameMode.Jizo => "地蔵スタイル賞", 
				GameMode.ChickenRace => "チキンレース", 
				GameMode.Symmetry => "シンメトリー", 
				GameMode.Wild => "Wild", 
				_ => "勝負なし (通常)", 
			};
		}
		set
		{
			switch (value)
			{
			case "運動量ランキング":
				PluginConfig.Instance.SelectedGameMode = 1;
				break;
			case "地蔵スタイル賞":
				PluginConfig.Instance.SelectedGameMode = 2;
				break;
			case "チキンレース":
				PluginConfig.Instance.SelectedGameMode = 3;
				break;
			case "シンメトリー":
				PluginConfig.Instance.SelectedGameMode = 4;
				break;
			case "Wild":
				PluginConfig.Instance.SelectedGameMode = 5;
				break;
			default:
				PluginConfig.Instance.SelectedGameMode = 0;
				break;
			}
			ResultsUIHost.Instance.UpdateTabContents();
		}
	}

	[UIValue("target-score")]
	public int TargetScore
	{
		get
		{
			return (int)PluginConfig.Instance.TargetScorePercentage;
		}
		set
		{
			PluginConfig.Instance.TargetScorePercentage = value;
			ResultsUIHost.Instance.UpdateTabContents();
		}
	}

	[UIAction("ask-daily-reset")]
	public void OnAskDailyReset()
	{
		_pendingTotalReset = false;
		SetConfirmState(showConfirm: true, "<color=#FFD700>⚠\ufe0f 当日得点をリセットしますか？</color>");
	}

	[UIAction("ask-total-reset")]
	public void OnAskTotalReset()
	{
		_pendingTotalReset = true;
		SetConfirmState(showConfirm: true, "<color=#FF5555>⚠\ufe0f 通算得点をリセットしますか？</color>");
	}

	[UIAction("confirm-reset-click")]
	public void OnConfirmResetClick()
	{
		SetConfirmState(showConfirm: false, "");
		if (_pendingTotalReset)
		{
			OnResetTotalClick();
		}
		else
		{
			OnResetDailyClick();
		}
	}

	[UIAction("cancel-reset-click")]
	public void OnCancelResetClick()
	{
		SetConfirmState(showConfirm: false, "");
	}

	private void SetConfirmState(bool showConfirm, string message)
	{
		if (_gpResetNormalRow != null)
		{
			_gpResetNormalRow.gameObject.SetActive(!showConfirm);
		}
		if (_gpResetConfirmRow != null)
		{
			_gpResetConfirmRow.gameObject.SetActive(showConfirm);
		}
		if (_gpResetConfirmText != null && showConfirm)
		{
			_gpResetConfirmText.text = message;
		}
		if (_stResetNormalRow != null)
		{
			_stResetNormalRow.gameObject.SetActive(!showConfirm);
		}
		if (_stResetConfirmRow != null)
		{
			_stResetConfirmRow.gameObject.SetActive(showConfirm);
		}
		if (_stResetConfirmText != null && showConfirm)
		{
			_stResetConfirmText.text = message;
		}
	}

	[UIAction("reset-daily-click")]
	public void OnResetDailyClick()
	{
		SessionStore.ResetDaily();
		ResultsUIHost.Instance.ClearLastEarnedPoints();
		ResultsUIHost.Instance.UpdateTabContents();
		ResultsUIHost.Instance.SwitchTab(0);
		ResultsUIHost.Instance.PublishCurrentStateToCloud(isSongEndUpdate: false);
		OnExternalTabSwitched(0);
		Plugin.Log.Info("Daily scores reset by user.");
	}

	[UIAction("reset-total-click")]
	public void OnResetTotalClick()
	{
		SessionStore.ResetTotal();
		ResultsUIHost.Instance.ClearLastEarnedPoints();
		ResultsUIHost.Instance.UpdateTabContents();
		ResultsUIHost.Instance.SwitchTab(1);
		ResultsUIHost.Instance.PublishCurrentStateToCloud(isSongEndUpdate: false);
		OnExternalTabSwitched(1);
		Plugin.Log.Info("Total scores reset by user.");
	}

	[UIAction("lobby-tab-0")]
	public void OnLobbyTab0()
	{
		ResultsUIHost.Instance.SwitchTab(0);
	}

	[UIAction("lobby-tab-1")]
	public void OnLobbyTab1()
	{
		ResultsUIHost.Instance.SwitchTab(1);
	}

	[UIAction("lobby-tab-2")]
	public void OnLobbyTab2()
	{
		ResultsUIHost.Instance.SwitchTab(2);
	}

	[UIAction("lobby-tab-3")]
	public void OnLobbyTab3()
	{
		ResultsUIHost.Instance.SwitchTab(3);
	}

	[UIAction("lobby-tab-4")]
	public void OnLobbyTab4()
	{
		ResultsUIHost.Instance.SwitchTab(4);
	}

	[UIAction("lobby-tab-5")]
	public void OnLobbyTab5()
	{
		ResultsUIHost.Instance.SwitchTab(5);
	}

	[UIAction("lobby-tab-6")]
	public void OnLobbyTab6()
	{
		ResultsUIHost.Instance.SwitchTab(6);
	}

	[UIAction("#post-parse")]
	public void OnPostParse()
	{
		ResultsUIHost.Instance.UpdateTabContents();
		OnExternalTabSwitched(0);
	}

	public void OnExternalTabSwitched(int tabIndex)
	{
		if (_lobbyRankingText != null)
		{
			_lobbyRankingText.text = ResultsUIHost.Instance.GetLobbyTabContent(tabIndex);
		}
	}

	public void Initialize()
	{
		if (!_isInitialized)
		{
			_isInitialized = true;
			BSMLCompat.AddSettingsMenu("MoreFunMultiplay", "MoreFunMultiplay.UI.Settings.bsml", this);
			BSMLCompat.AddGameplaySetupTab("MoreFunMultiplay", "MoreFunMultiplay.UI.GameplaySetup.bsml", this);
		}
	}
}
