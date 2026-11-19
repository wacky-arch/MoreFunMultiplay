using System;
using System.Reflection;
using HarmonyLib;

namespace MoreFunMultiplay;

[HarmonyPatch]
public class SoloAndMultiPlusResultsPatch
{
	private static FieldInfo _resultsField;

	[HarmonyPrepare]
	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("ResultsViewController");
		if (!(type != null))
		{
			return null;
		}
		return AccessTools.Method(type, "DidActivate", (Type[])null, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix(object __instance)
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
			LevelCompletionResults results = _resultsField?.GetValue(__instance) as LevelCompletionResults;
			ResultsUIHost.Instance.ProcessSongEndIfNeeded(results);
			ResultsUIHost.Instance.ShowFloatingScreen();
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("Error in SoloAndMultiPlusResultsPatch: " + ex);
		}
	}
}
