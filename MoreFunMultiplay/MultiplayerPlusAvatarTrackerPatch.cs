using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MoreFunMultiplay;

[HarmonyPatch]
public class MultiplayerPlusAvatarTrackerPatch
{
	private static FieldInfo _headField;

	private static FieldInfo _leftField;

	private static FieldInfo _rightField;

	private static PropertyInfo _luidProp;

	[HarmonyPrepare]
	private static bool Prepare()
	{
		return CalculateMethod() != null;
	}

	[HarmonyTargetMethod]
	private static MethodBase CalculateMethod()
	{
		Type type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Components.RemotePlayerAvatarController");
		if (type == null)
		{
			return null;
		}
		return AccessTools.Method(type, "LateUpdate", (Type[])null, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix(Component __instance)
	{
		try
		{
			if (MultiplayManager.Instance == null || !MultiplayManager.Instance.IsInSong || !MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
			{
				return;
			}
			if (_headField == null)
			{
				Type type = __instance.GetType();
				_headField = AccessTools.Field(type, "m_HeadTransform");
				_leftField = AccessTools.Field(type, "m_LeftHandTransform");
				_rightField = AccessTools.Field(type, "m_RightHandTransform");
				_luidProp = AccessTools.Property(type, "PlayerLUID");
			}
			Transform transform = _headField?.GetValue(__instance) as Transform;
			Transform transform2 = _leftField?.GetValue(__instance) as Transform;
			Transform transform3 = _rightField?.GetValue(__instance) as Transform;
			if (transform == null || transform2 == null || transform3 == null)
			{
				return;
			}
			uint num = 0u;
			if (_luidProp != null && _luidProp.GetValue(__instance) is uint num2)
			{
				num = num2;
			}
			if (num != 0)
			{
				string playerNameByLUID = MultiplayerPlusBridge.GetPlayerNameByLUID(num);
				if (!string.IsNullOrEmpty(playerNameByLUID))
				{
					MultiplayManager.Instance.RegisterPlayer(playerNameByLUID, transform, transform2, transform3);
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("Error in MultiplayerPlusAvatarTrackerPatch: " + ex);
		}
	}
}
