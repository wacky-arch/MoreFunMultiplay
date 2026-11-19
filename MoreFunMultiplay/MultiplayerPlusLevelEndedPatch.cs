using System;
using System.Reflection;
using HarmonyLib;

namespace MoreFunMultiplay;

[HarmonyPatch]
public class MultiplayerPlusLevelEndedPatch
{
	private static PropertyInfo _resultsProp;

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
		return AccessTools.Method(type, "Logic_OnLevelEnded", (Type[])null, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix(object[] __args)
	{
		try
		{
			if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
			{
				return;
			}
			LevelCompletionResults results = null;
			if (__args != null && __args.Length != 0 && __args[0] != null)
			{
				if (_resultsProp == null)
				{
					_resultsProp = AccessTools.Property(__args[0].GetType(), "Results");
				}
				results = _resultsProp?.GetValue(__args[0]) as LevelCompletionResults;
			}
			ResultsUIHost.Instance.ProcessSongEndIfNeeded(results);
			ResultsUIHost.Instance.ShowFloatingScreen();
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("Error in MultiplayerPlusLevelEndedPatch: " + ex);
		}
	}
}
