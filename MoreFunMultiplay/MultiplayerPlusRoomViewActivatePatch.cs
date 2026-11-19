using System;
using System.Reflection;
using HarmonyLib;

namespace MoreFunMultiplay;

[HarmonyPatch]
public class MultiplayerPlusRoomViewActivatePatch
{
	[HarmonyPrepare]
	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.UI.MultiplayerPRoomMainView");
		if (type == null)
		{
			return null;
		}
		return AccessTools.Method(type, "OnViewActivation", (Type[])null, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix()
	{
		try
		{
			if (MultiplayerPlusBridge.IsInMultiplayerPlusRoom() && (MultiplayManager.Instance == null || !MultiplayManager.Instance.IsInSong))
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
