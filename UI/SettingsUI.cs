using System;
using System.Collections.Generic;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Settings;
using TMPro;
using UnityEngine;

namespace MoreFunMultiplay
{
    /// <summary>
    /// Beat Saber 1.29.1 (.instance) と 1.37+ / 1.40.8 / 1.44.1 (.Instance) の両方で動作するBSML互換ヘルパー
    /// </summary>
    public static class BSMLCompat
    {
        private static object GetSingleton(Type type)
        {
            var prop = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    ?? type.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return prop?.GetValue(null);
        }

        public static void Parse(string bsml, GameObject parent, object host)
        {
            try
            {
                var type = typeof(BSMLParser);
                var inst = GetSingleton(type);
                var method = type.GetMethod("Parse", new[] { typeof(string), typeof(GameObject), typeof(object) });
                method?.Invoke(inst, new object[] { bsml, parent, host });
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("BSMLCompat.Parse error: " + ex);
            }
        }

        public static void AddSettingsMenu(string name, string resource, object host)
        {
            try
            {
                var type = typeof(BSMLSettings);
                var inst = GetSingleton(type);
                var method = type.GetMethod("AddSettingsMenu", new[] { typeof(string), typeof(string), typeof(object) });
                method?.Invoke(inst, new object[] { name, resource, host });
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("BSMLCompat.AddSettingsMenu error: " + ex);
            }
        }

        public static void AddGameplaySetupTab(string name, string resource, object host)
        {
            try
            {
                var type = typeof(GameplaySetup);
                var inst = GetSingleton(type);
                var method4 = type.GetMethod("AddTab", new[] { typeof(string), typeof(string), typeof(object), typeof(MenuType) });
                if (method4 != null)
                {
                    method4.Invoke(inst, new object[] { name, resource, host, MenuType.All });
                    return;
                }
                var method3 = type.GetMethod("AddTab", new[] { typeof(string), typeof(string), typeof(object) });
                method3?.Invoke(inst, new object[] { name, resource, host });
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("BSMLCompat.AddGameplaySetupTab error: " + ex);
            }
        }
    }
}

namespace MoreFunMultiplay.UI
{
    public class SettingsUI
    {
        public static SettingsUI Instance { get; } = new SettingsUI();

        private bool _isInitialized = false;
        private bool _pendingTotalReset = false;

        [UIComponent("lobby-ranking-text")]
        private TextMeshProUGUI _lobbyRankingText = null;

        [UIComponent("gp-reset-normal-row")]
        private RectTransform _gpResetNormalRow = null;

        [UIComponent("gp-reset-confirm-row")]
        private RectTransform _gpResetConfirmRow = null;

        [UIComponent("gp-reset-confirm-text")]
        private TextMeshProUGUI _gpResetConfirmText = null;

        [UIComponent("st-reset-normal-row")]
        private RectTransform _stResetNormalRow = null;

        [UIComponent("st-reset-confirm-row")]
        private RectTransform _stResetConfirmRow = null;

        [UIComponent("st-reset-confirm-text")]
        private TextMeshProUGUI _stResetConfirmText = null;

        [UIValue("game-mode-options")]
        public List<object> GameModeOptions = new List<object>
        {
            "勝負なし (通常)",
            "運動量ランキング",
            "地蔵スタイル賞",
            "チキンレース"
        };

        [UIValue("game-mode")]
        public string CurrentGameModeStr
        {
            get
            {
                switch ((GameMode)PluginConfig.Instance.SelectedGameMode)
                {
                    case GameMode.Sweat: return "運動量ランキング";
                    case GameMode.Jizo: return "地蔵スタイル賞";
                    case GameMode.ChickenRace: return "チキンレース";
                    default: return "勝負なし (通常)";
                }
            }
            set
            {
                if (value == "運動量ランキング") PluginConfig.Instance.SelectedGameMode = (int)GameMode.Sweat;
                else if (value == "地蔵スタイル賞") PluginConfig.Instance.SelectedGameMode = (int)GameMode.Jizo;
                else if (value == "チキンレース") PluginConfig.Instance.SelectedGameMode = (int)GameMode.ChickenRace;
                else PluginConfig.Instance.SelectedGameMode = (int)GameMode.None;
                ResultsUIHost.Instance.UpdateTabContents();
            }
        }

        [UIValue("target-score")]
        public int TargetScore
        {
            get => (int)PluginConfig.Instance.TargetScorePercentage;
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
            SetConfirmState(true, "<color=#FFD700>⚠️ 当日得点をリセットしますか？</color>");
        }

        [UIAction("ask-total-reset")]
        public void OnAskTotalReset()
        {
            _pendingTotalReset = true;
            SetConfirmState(true, "<color=#FF5555>⚠️ 通算得点をリセットしますか？</color>");
        }

        [UIAction("confirm-reset-click")]
        public void OnConfirmResetClick()
        {
            SetConfirmState(false, "");
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
            SetConfirmState(false, "");
        }

        private void SetConfirmState(bool showConfirm, string message)
        {
            if (_gpResetNormalRow != null) _gpResetNormalRow.gameObject.SetActive(!showConfirm);
            if (_gpResetConfirmRow != null) _gpResetConfirmRow.gameObject.SetActive(showConfirm);
            if (_gpResetConfirmText != null && showConfirm) _gpResetConfirmText.text = message;

            if (_stResetNormalRow != null) _stResetNormalRow.gameObject.SetActive(!showConfirm);
            if (_stResetConfirmRow != null) _stResetConfirmRow.gameObject.SetActive(showConfirm);
            if (_stResetConfirmText != null && showConfirm) _stResetConfirmText.text = message;
        }

        [UIAction("reset-daily-click")]
        public void OnResetDailyClick()
        {
            SessionStore.ResetDaily();
            ResultsUIHost.Instance.ClearLastEarnedPoints();
            ResultsUIHost.Instance.UpdateTabContents();
            ResultsUIHost.Instance.SwitchTab(0);
            ResultsUIHost.Instance.PublishCurrentStateToCloud(false);
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
            ResultsUIHost.Instance.PublishCurrentStateToCloud(false);
            OnExternalTabSwitched(1);
            Plugin.Log.Info("Total scores reset by user.");
        }

        [UIAction("lobby-tab-0")]
        public void OnLobbyTab0() => ResultsUIHost.Instance.SwitchTab(0);

        [UIAction("lobby-tab-1")]
        public void OnLobbyTab1() => ResultsUIHost.Instance.SwitchTab(1);

        [UIAction("lobby-tab-2")]
        public void OnLobbyTab2() => ResultsUIHost.Instance.SwitchTab(2);

        [UIAction("lobby-tab-3")]
        public void OnLobbyTab3() => ResultsUIHost.Instance.SwitchTab(3);

        [UIAction("lobby-tab-4")]
        public void OnLobbyTab4() => ResultsUIHost.Instance.SwitchTab(4);

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
            if (_isInitialized) return;
            _isInitialized = true;

            BSMLCompat.AddSettingsMenu("MoreFunMultiplay", "MoreFunMultiplay.UI.Settings.bsml", this);
            BSMLCompat.AddGameplaySetupTab("MoreFunMultiplay", "MoreFunMultiplay.UI.GameplaySetup.bsml", this);
        }
    }
}
