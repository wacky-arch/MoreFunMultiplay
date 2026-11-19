using System;
using System.Reflection;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Logging;
using MoreFunMultiplay.UI;
using UnityEngine;

namespace MoreFunMultiplay;

[Plugin(RuntimeOptions.SingleStartInit)]
public class Plugin
{
	private Harmony _harmony;

	internal static Plugin Instance { get; private set; }

	internal static IPA.Logging.Logger Log { get; private set; }

	[Init]
	public void Init(IPA.Logging.Logger logger, Config conf)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		Instance = this;
		Log = logger;
		PluginConfig.Instance = conf.Generated<PluginConfig>();
		PluginConfig.Instance.SelectedGameMode = 0;
		_harmony = new Harmony("com.wacky.BeatSaber.MoreFunMultiplay");
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
			GameObject gameObject = new GameObject("MoreFunMultiplayManager");
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<MultiplayManager>();
			EventInfo eventInfo = AccessTools.TypeByName("BeatSaberMarkupLanguage.Util.MainMenuAwaiter")?.GetEvent("MainMenuInitializing", BindingFlags.Static | BindingFlags.Public);
			if (eventInfo != null)
			{
				eventInfo.AddEventHandler(null, new Action(OnMainMenuInitializing));
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
