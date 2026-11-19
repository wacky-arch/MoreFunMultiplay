using System;
using System.Reflection;
using HarmonyLib;

namespace MoreFunMultiplay;

[HarmonyPatch]
public class MainMenuDeactivateFloatingScreenPatch
{
	[HarmonyPrepare]
	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("MainMenuViewController");
		if (!(type != null))
		{
			return null;
		}
		return AccessTools.Method(type, "DidActivate", (Type[])null, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix()
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
