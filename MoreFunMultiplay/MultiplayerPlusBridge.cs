using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace MoreFunMultiplay;

public static class MultiplayerPlusBridge
{
	private static Type _networkManagerType;

	private static PropertyInfo _statusProp;

	private static PropertyInfo _selfPlayerProp;

	private static PropertyInfo _roomDataProp;

	private static FieldInfo _roomDataField;

	private static PropertyInfo _roomPlayersProp;

	private static FieldInfo _roomPlayersDictField;

	private static FieldInfo _userNameField;

	private static FieldInfo _luidField;

	private static FieldInfo _accuracyField;

	private static FieldInfo _hitCountField;

	private static FieldInfo _missCountField;

	private static FieldInfo _failedField;

	private static FieldInfo _spectateField;

	private static FieldInfo _roomCodeField;

	private static FieldInfo _roomIdField;

	private static FieldInfo _hostLuidField;

	private static bool _initialized = false;

	private static readonly Dictionary<uint, string> _luidNameCache = new Dictionary<uint, string>();

	private static void EnsureInitialized()
	{
		if (!_initialized)
		{
			_initialized = true;
			_networkManagerType = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Network.NetworkManager");
			if (_networkManagerType != null)
			{
				_statusProp = AccessTools.Property(_networkManagerType, "Status");
				_selfPlayerProp = AccessTools.Property(_networkManagerType, "SelfPlayer");
				_roomDataProp = AccessTools.Property(_networkManagerType, "RoomData");
				_roomDataField = AccessTools.Field(_networkManagerType, "m_RoomData");
				_roomPlayersProp = AccessTools.Property(_networkManagerType, "RoomPlayersLUnsafe");
				_roomPlayersDictField = AccessTools.Field(_networkManagerType, "m_RoomPlayersD");
			}
			Type type = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Network.Types.PlayerData");
			if (type != null)
			{
				_userNameField = AccessTools.Field(type, "UserName");
				_luidField = AccessTools.Field(type, "LUID");
				_accuracyField = AccessTools.Field(type, "Accuracy");
				_hitCountField = AccessTools.Field(type, "HitCount");
				_missCountField = AccessTools.Field(type, "MissCount");
				_failedField = AccessTools.Field(type, "Failed");
				_spectateField = AccessTools.Field(type, "Spectate");
			}
			Type type2 = AccessTools.TypeByName("BeatSaberPlus_Multiplayer.Network.Types.RoomData");
			if (type2 != null)
			{
				_roomCodeField = AccessTools.Field(type2, "RoomCode");
				_roomIdField = AccessTools.Field(type2, "RoomID");
				_hostLuidField = AccessTools.Field(type2, "HostLUID");
			}
		}
	}

	private static object GetRoomDataObject()
	{
		EnsureInitialized();
		if (_roomDataProp != null)
		{
			object value = _roomDataProp.GetValue(null);
			if (value != null)
			{
				return value;
			}
		}
		if (_roomDataField != null)
		{
			return _roomDataField.GetValue(null);
		}
		return null;
	}

	public static string GetCurrentRoomCode()
	{
		try
		{
			object roomDataObject = GetRoomDataObject();
			if (roomDataObject != null)
			{
				string text = _roomCodeField?.GetValue(roomDataObject) as string;
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text.Trim().ToUpperInvariant();
				}
				string text2 = _roomIdField?.GetValue(roomDataObject) as string;
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2.Trim().ToUpperInvariant();
				}
			}
		}
		catch
		{
		}
		return "";
	}

	public static bool IsLocalPlayerRoomHost()
	{
		try
		{
			object roomDataObject = GetRoomDataObject();
			object obj = _selfPlayerProp?.GetValue(null);
			if (roomDataObject != null && obj != null && _hostLuidField != null && _luidField != null)
			{
				uint num = Convert.ToUInt32(_hostLuidField.GetValue(roomDataObject));
				uint num2 = Convert.ToUInt32(_luidField.GetValue(obj));
				if (num != 0 && num2 != 0)
				{
					return num == num2;
				}
			}
		}
		catch
		{
		}
		return true;
	}

	public static string GetRoomHostName()
	{
		try
		{
			if (IsLocalPlayerRoomHost())
			{
				return GetLocalPlayerName();
			}
			object roomDataObject = GetRoomDataObject();
			if (roomDataObject != null && _hostLuidField != null)
			{
				uint num = Convert.ToUInt32(_hostLuidField.GetValue(roomDataObject));
				if (num != 0)
				{
					string playerNameByLUID = GetPlayerNameByLUID(num);
					if (!string.IsNullOrEmpty(playerNameByLUID))
					{
						return playerNameByLUID;
					}
				}
			}
		}
		catch
		{
		}
		return "Host";
	}

	public static bool IsInMultiplayerPlusRoom()
	{
		try
		{
			EnsureInitialized();
			if (_statusProp != null)
			{
				object value = _statusProp.GetValue(null);
				if (value != null && Convert.ToInt32(value) == 6)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static string GetLocalPlayerName()
	{
		try
		{
			EnsureInitialized();
			if (_selfPlayerProp != null && _userNameField != null)
			{
				object value = _selfPlayerProp.GetValue(null);
				if (value != null)
				{
					string text = _userNameField.GetValue(value) as string;
					if (!string.IsNullOrEmpty(text))
					{
						return text;
					}
				}
			}
		}
		catch
		{
		}
		return "You";
	}

	public static string GetPlayerNameByLUID(uint luid)
	{
		try
		{
			EnsureInitialized();
			if (_roomPlayersDictField != null && _userNameField != null && _roomPlayersDictField.GetValue(null) is IDictionary dictionary && dictionary.Contains(luid))
			{
				object obj = dictionary[luid];
				if (obj != null)
				{
					string text = _userNameField.GetValue(obj) as string;
					if (!string.IsNullOrEmpty(text))
					{
						_luidNameCache[luid] = text;
						return text;
					}
				}
			}
			if (_roomPlayersProp != null && _luidField != null && _userNameField != null && _roomPlayersProp.GetValue(null) is IList { Count: var count } list)
			{
				for (int i = 0; i < count; i++)
				{
					object obj2 = list[i];
					if (obj2 != null && _luidField.GetValue(obj2) is uint num && num == luid)
					{
						string text2 = _userNameField.GetValue(obj2) as string;
						if (!string.IsNullOrEmpty(text2))
						{
							_luidNameCache[luid] = text2;
							return text2;
						}
					}
				}
			}
		}
		catch
		{
		}
		if (_luidNameCache.TryGetValue(luid, out var value))
		{
			return value;
		}
		return null;
	}

	public static void CollectRoomScores(List<PlayerScoreData> output)
	{
		try
		{
			EnsureInitialized();
			if (_userNameField == null || _accuracyField == null)
			{
				return;
			}
			if (_selfPlayerProp != null)
			{
				AddPlayerDataScore(_selfPlayerProp.GetValue(null), output);
			}
			if (_roomPlayersDictField != null && _roomPlayersDictField.GetValue(null) is IDictionary dictionary)
			{
				{
					foreach (object value in dictionary.Values)
					{
						AddPlayerDataScore(value, output);
					}
					return;
				}
			}
			if (_roomPlayersProp != null && _roomPlayersProp.GetValue(null) is IList { Count: var count } list)
			{
				for (int i = 0; i < count; i++)
				{
					AddPlayerDataScore(list[i], output);
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("Error in CollectRoomScores: " + ex);
		}
	}

	private static void AddPlayerDataScore(object playerData, List<PlayerScoreData> output)
	{
		if (playerData == null)
		{
			return;
		}
		bool flag = default(bool);
		int num;
		if (_spectateField != null)
		{
			object value = _spectateField.GetValue(playerData);
			if (value is bool)
			{
				flag = (bool)value;
				num = 1;
			}
			else
			{
				num = 0;
			}
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) != 0)
		{
			return;
		}
		string name = _userNameField?.GetValue(playerData) as string;
		if (string.IsNullOrEmpty(name) || !(_accuracyField?.GetValue(playerData) is float num2) || !(num2 >= 0f))
		{
			return;
		}
		float num3 = ((num2 <= 1f) ? (num2 * 100f) : num2);
		int num4 = 0;
		int num5 = 0;
		bool flag2 = false;
		try
		{
			if (_hitCountField != null)
			{
				num4 = Convert.ToInt32(_hitCountField.GetValue(playerData));
			}
			if (_missCountField != null)
			{
				num5 = Convert.ToInt32(_missCountField.GetValue(playerData));
			}
			if (_failedField != null && _failedField.GetValue(playerData) is bool flag3)
			{
				flag2 = flag3;
			}
		}
		catch
		{
		}
		PlayerScoreData playerScoreData = default(PlayerScoreData);
		playerScoreData.Name = name;
		playerScoreData.Percent = num3;
		playerScoreData.HitCount = num4;
		playerScoreData.MissCount = num5;
		playerScoreData.Failed = flag2;
		PlayerScoreData playerScoreData2 = playerScoreData;
		int num6 = output.FindIndex((PlayerScoreData x) => x.Name == name);
		if (num6 < 0)
		{
			output.Add(playerScoreData2);
			return;
		}
		if (output[num6].Percent <= 0f && num3 > 0f)
		{
			output[num6] = playerScoreData2;
			return;
		}
		PlayerScoreData value2 = output[num6];
		if (num5 > value2.MissCount)
		{
			value2.MissCount = num5;
		}
		if (num4 > value2.HitCount)
		{
			value2.HitCount = num4;
		}
		if (flag2)
		{
			value2.Failed = true;
		}
		output[num6] = value2;
	}
}
