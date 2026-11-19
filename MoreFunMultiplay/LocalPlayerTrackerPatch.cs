using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MoreFunMultiplay;

[HarmonyPatch(typeof(PlayerTransforms))]
public class LocalPlayerTrackerPatch
{
	private static FieldInfo _headField;

	private static FieldInfo _leftField;

	private static FieldInfo _rightField;

	[HarmonyPatch("Update")]
	[HarmonyPostfix]
	private static void UpdatePostfix(PlayerTransforms __instance)
	{
		try
		{
			if (!(MultiplayManager.Instance == null) && MultiplayManager.Instance.IsInSong && MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
			{
				if (_headField == null)
				{
					Type typeFromHandle = typeof(PlayerTransforms);
					_headField = AccessTools.Field(typeFromHandle, "_headTransform");
					_leftField = AccessTools.Field(typeFromHandle, "_leftHandTransform");
					_rightField = AccessTools.Field(typeFromHandle, "_rightHandTransform");
				}
				Transform transform = _headField?.GetValue(__instance) as Transform;
				Transform transform2 = _leftField?.GetValue(__instance) as Transform;
				Transform transform3 = _rightField?.GetValue(__instance) as Transform;
				if (!(transform == null) && !(transform2 == null) && !(transform3 == null))
				{
					string localPlayerName = MultiplayerPlusBridge.GetLocalPlayerName();
					MultiplayManager.Instance.LocalPlayerName = localPlayerName;
					MultiplayManager.Instance.RegisterPlayer(localPlayerName, transform, transform2, transform3);
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("Error in LocalPlayerTrackerPatch.UpdatePostfix: " + ex);
		}
	}
}
