using HarmonyLib;

namespace MoreFunMultiplay;

[HarmonyPatch(typeof(RelativeScoreAndImmediateRankCounter), "UpdateRelativeScoreAndImmediateRank")]
public class RelativeScoreTrackerPatch
{
	[HarmonyPostfix]
	private static void Postfix(RelativeScoreAndImmediateRankCounter __instance)
	{
		if (MultiplayManager.Instance != null && MultiplayManager.Instance.IsInSong && MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
		{
			MultiplayManager.Instance.LocalPlayerAccuracyPercent = __instance.relativeScore * 100f;
		}
	}
}
