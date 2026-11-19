using System;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Settings;
using UnityEngine;

namespace MoreFunMultiplay;

public static class BSMLCompat
{
	private static object GetSingleton(Type type)
	{
		return (type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy) ?? type.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy))?.GetValue(null);
	}

	public static void Parse(string bsml, GameObject parent, object host)
	{
		try
		{
			Type typeFromHandle = typeof(BSMLParser);
			object singleton = GetSingleton(typeFromHandle);
			typeFromHandle.GetMethod("Parse", new Type[3]
			{
				typeof(string),
				typeof(GameObject),
				typeof(object)
			})?.Invoke(singleton, new object[3] { bsml, parent, host });
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("BSMLCompat.Parse error: " + ex);
		}
	}

	public static void AddSettingsMenu(string name, string resource, object host)
	{
		try
		{
			Type typeFromHandle = typeof(BSMLSettings);
			object singleton = GetSingleton(typeFromHandle);
			typeFromHandle.GetMethod("AddSettingsMenu", new Type[3]
			{
				typeof(string),
				typeof(string),
				typeof(object)
			})?.Invoke(singleton, new object[3] { name, resource, host });
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("BSMLCompat.AddSettingsMenu error: " + ex);
		}
	}

	public static void AddGameplaySetupTab(string name, string resource, object host)
	{
		try
		{
			Type typeFromHandle = typeof(GameplaySetup);
			object singleton = GetSingleton(typeFromHandle);
			MethodInfo method = typeFromHandle.GetMethod("AddTab", new Type[4]
			{
				typeof(string),
				typeof(string),
				typeof(object),
				typeof(MenuType)
			});
			if (method != null)
			{
				method.Invoke(singleton, new object[4]
				{
					name,
					resource,
					host,
					MenuType.All
				});
			}
			else
			{
				typeFromHandle.GetMethod("AddTab", new Type[3]
				{
					typeof(string),
					typeof(string),
					typeof(object)
				})?.Invoke(singleton, new object[3] { name, resource, host });
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("BSMLCompat.AddGameplaySetupTab error: " + ex);
		}
	}
}
