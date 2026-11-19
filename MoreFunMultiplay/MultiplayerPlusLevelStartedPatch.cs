using System;
using System.Reflection;
using HarmonyLib;

namespace MoreFunMultiplay;

[HarmonyPatch]
public class MultiplayerPlusLevelStartedPatch
{
	[HarmonyPrepare]
	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Managers.GameplayManager");
		if (type == null)
		{
			return null;
		}
		return AccessTools.Method(type, "Logic_OnLevelStarted", (Type[])null, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix()
	{
		try
		{
			if (MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
			{
				ResultsUIHost.Instance.HideFloatingScreen();
				if (MultiplayManager.Instance != null)
				{
					MultiplayManager.Instance.StartSong();
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("Error in MultiplayerPlusLevelStartedPatch: " + ex);
		}
	}
}
