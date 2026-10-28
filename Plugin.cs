using System;
using System.Reflection;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using MoreFunMultiplay.UI;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace MoreFunMultiplay
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        private HarmonyLib.Harmony _harmony;

        [Init]
        public void Init(IPALogger logger, IPA.Config.Config conf)
        {
            Instance = this;
            Log = logger;
            PluginConfig.Instance = conf.Generated<PluginConfig>();
            // 起動時は毎回必ず「勝負なし (GameMode.None)」にリセット（野良マルチ等での誤加算防止）
            PluginConfig.Instance.SelectedGameMode = (int)GameMode.None;
            _harmony = new HarmonyLib.Harmony("com.wacky.BeatSaber.MoreFunMultiplay");
            Log.Info("MoreFunMultiplay initialized.");
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Info("MoreFunMultiplay starting...");

            try
            {
                SessionStore.Load();

                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                var managerGo = new GameObject("MoreFunMultiplayManager");
                UnityEngine.Object.DontDestroyOnLoad(managerGo);
                managerGo.AddComponent<MultiplayManager>();

                // BSML 1.37+ (MainMenuAwaiter) が存在する場合はイベント購読、1.29.1等の場合は直接初期化＋MainMenuパッチで補完
                var awaiterType = AccessTools.TypeByName("BeatSaberMarkupLanguage.Util.MainMenuAwaiter");
                var evt = awaiterType?.GetEvent("MainMenuInitializing", BindingFlags.Public | BindingFlags.Static);
                if (evt != null)
                {
                    evt.AddEventHandler(null, new Action(OnMainMenuInitializing));
                }
                else
                {
                    OnMainMenuInitializing();
                }

                Log.Info("MoreFunMultiplay started successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("Error starting MoreFunMultiplay: " + ex);
            }
        }

        internal static void OnMainMenuInitializing()
        {
            try
            {
                SettingsUI.Instance.Initialize();
                Log.Info("MoreFunMultiplay BSML Settings & GameplaySetup registered.");
            }
            catch (Exception ex)
            {
                Log.Error("Error initializing MoreFunMultiplay UI: " + ex);
            }
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            SessionStore.Save();
            Log.Info("MoreFunMultiplay exited.");
        }
    }
}
