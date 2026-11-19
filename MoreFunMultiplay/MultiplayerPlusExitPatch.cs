using System;
using System.Reflection;
using HarmonyLib;

namespace MoreFunMultiplay;

[HarmonyPatch]
public class MultiplayerPlusExitPatch
{
	[HarmonyPrepare]
	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Managers.RoomManager");
		if (type == null)
		{
			return null;
		}
		return AccessTools.Method(type, "OnExitMultiplayer", (Type[])null, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix()
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
