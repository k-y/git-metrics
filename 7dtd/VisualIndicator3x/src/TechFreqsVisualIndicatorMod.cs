using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace TechFreqsVisualIndicatorMod;

public class TechFreqsVisualIndicatorMod : IModApi
{
	[Serializable]
	private class Config
	{
		public string ToggleKey { get; set; } = "Semicolon";

		public float? DetectionRadius { get; set; } = 50f;

		public float? UpdateInterval { get; set; } = 3f;

		public bool? DebugLogging { get; set; } = false;

		public bool? ShowDistance { get; set; } = true;

		public bool? ShowLabels { get; set; } = true;

		public bool? ShowCompassIcons { get; set; } = true;

		public bool? ShowOnScreenIcons { get; set; } = true;

		public bool? ShowMapIcons { get; set; } = true;

		public bool? AutoEnable { get; set; } = true;
	}

	[HarmonyPatch(typeof(EntityPlayerLocal), "Update")]
	private class TogglePatch
	{
		private static void Postfix(EntityPlayerLocal __instance)
		{
			if (!((Object)(object)__instance == (Object)null) && !((Entity)__instance).isEntityRemote && ((Entity)__instance).IsSpawned() && Input.GetKeyDown(ToggleKey))
			{
				IndicatorsEnabled = !IndicatorsEnabled;
				GameManager.ShowTooltip(__instance, "[TechFreqs Visual Indicator] " + (IndicatorsEnabled ? "ENABLED" : "DISABLED"), false, false, 0f);
				if (!IndicatorsEnabled)
				{
					DisableDetector();
				}
			}
		}
	}

	public const string MOD_PREFIX = "[TechFreqs Visual Indicator] ";

	private static string configPath;

	private static DateTime lastConfigWriteTime = DateTime.MinValue;

	private static Mod _modInstance;

	private static readonly Dictionary<string, NavObject> entityNavObjects = new Dictionary<string, NavObject>();

	public static bool IndicatorsEnabled { get; private set; } = true;

	public static float DetectionRadius { get; private set; } = 50f;

	public static KeyCode ToggleKey { get; private set; } = (KeyCode)59;

	public static float UpdateInterval { get; private set; } = 3f;

	public static float StartDelay { get; private set; } = 5f;

	public static bool DebugLogging { get; private set; } = false;

	public static bool ShowDistance { get; private set; } = true;

	public static bool ShowLabels { get; private set; } = true;

	public static bool ShowCompassIcons { get; private set; } = true;

	public static bool ShowOnScreenIcons { get; private set; } = true;

	public static bool ShowMapIcons { get; private set; } = true;

	public static bool AutoEnable { get; private set; } = true;

	public void InitMod(Mod modInstance)
	{
		_modInstance = modInstance;
		configPath = Path.Combine(modInstance.Path, "config.json");
		Log("<color=cyan>TechFreqs Visual Indicator v3.0 LOADED</color>");
		new Harmony("com.techfreq.visualindicator").PatchAll();
		LoadConfig();
		IndicatorsEnabled = AutoEnable;
		((MonoBehaviour)GameManager.Instance).StartCoroutine(MainLoop());
	}

	private static IEnumerator MainLoop()
	{
		yield return (object)new WaitForSeconds(StartDelay);
		while (true)
		{
			CheckForConfigChangesAndReload();
			if (IndicatorsEnabled)
			{
				GameManager instance = GameManager.Instance;
				object obj;
				if (instance == null)
				{
					obj = null;
				}
				else
				{
					World world = instance.World;
					obj = ((world != null) ? ((WorldBase)world).GetPrimaryPlayer() : null);
				}
				EntityPlayerLocal val = (EntityPlayerLocal)obj;
				if ((Object)(object)val != (Object)null)
				{
					UpdateEntityDetector(val);
				}
			}
			yield return (object)new WaitForSeconds(UpdateInterval);
		}
	}

	private static void CheckForConfigChangesAndReload()
	{
		if (!File.Exists(configPath))
		{
			return;
		}
		try
		{
			DateTime lastWriteTime = File.GetLastWriteTime(configPath);
			if (!(lastWriteTime <= lastConfigWriteTime))
			{
				lastConfigWriteTime = lastWriteTime;
				LoadConfig();
				IndicatorsEnabled = AutoEnable;
				GameManager instance = GameManager.Instance;
				object obj;
				if (instance == null)
				{
					obj = null;
				}
				else
				{
					World world = instance.World;
					obj = ((world != null) ? ((WorldBase)world).GetPrimaryPlayer() : null);
				}
				EntityPlayerLocal val = (EntityPlayerLocal)obj;
				if ((Object)(object)val != (Object)null)
				{
					GameManager.ShowTooltip(val, "[TechFreqs Visual Indicator] Config Reloaded!", false, false, 0f);
				}
				Log("Config auto-reloaded");
			}
		}
		catch
		{
		}
	}

	private static void LoadConfig()
	{
		try
		{
			if (!File.Exists(configPath))
			{
				CreateDefaultConfig();
				return;
			}
			Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
			ToggleKey = (KeyCode)((!Enum.TryParse<KeyCode>(config.ToggleKey ?? "Semicolon", ignoreCase: true, out KeyCode result)) ? 59 : ((int)result));
			DetectionRadius = Mathf.Max(10f, config.DetectionRadius ?? 50f);
			UpdateInterval = Mathf.Clamp(config.UpdateInterval ?? 3f, 0.5f, 30f);
			DebugLogging = config.DebugLogging == true;
			ShowDistance = config.ShowDistance ?? true;
			ShowLabels = config.ShowLabels ?? true;
			ShowCompassIcons = config.ShowCompassIcons ?? true;
			ShowOnScreenIcons = config.ShowOnScreenIcons ?? true;
			ShowMapIcons = config.ShowMapIcons ?? true;
			AutoEnable = config.AutoEnable ?? true;
			Log("Config loaded successfully");
		}
		catch (Exception ex)
		{
			Log("Config error: " + ex.Message);
		}
	}

	private static void CreateDefaultConfig()
	{
		Config config = new Config();
		File.WriteAllText(configPath, JsonConvert.SerializeObject((object)config, (Formatting)1));
		Log("Default config.json created");
	}

	private static void UpdateEntityDetector(EntityPlayerLocal player)
	{
		if (((Entity)(player?)).world?.Entities?.dict == null || NavObjectManager.Instance == null)
		{
			return;
		}
		foreach (NavObject value2 in entityNavObjects.Values)
		{
			NavObjectManager.Instance.UnRegisterNavObject(value2);
		}
		entityNavObjects.Clear();
		foreach (KeyValuePair<int, Entity> item in ((Entity)player).world.Entities.dict)
		{
			Entity value = item.Value;
			EntityAlive val = (EntityAlive)(object)((value is EntityAlive) ? value : null);
			if (val != null && ((Entity)val).entityId != ((Entity)player).entityId && ((Entity)val).IsAlive() && !((Entity)val).IsDespawned && !(Vector3.Distance(((Entity)player).position, ((Entity)val).position) > DetectionRadius))
			{
				string key = $"entity_{((Entity)val).entityId}";
				CreateOrUpdateNavObject(player, key, val);
			}
		}
	}

	private static void CreateOrUpdateNavObject(EntityPlayerLocal player, string key, EntityAlive entity)
	{
		float num = Vector3.Distance(((Entity)player).position, ((Entity)entity).position);
		bool showCompassIcons = ShowCompassIcons;
		bool showOnScreenIcons = ShowOnScreenIcons;
		bool showMapIcons = ShowMapIcons;
		bool showLabels = ShowLabels;
		bool showDistance = ShowDistance;
		string name = "";
		if (showLabels)
		{
			string text = ((!string.IsNullOrEmpty(((Entity)entity).GetDebugName())) ? ((Entity)entity).GetDebugName() : ((Entity)entity).EntityClass.entityClassName);
			name = ((!showDistance) ? text : ((num <= 5f) ? (text + " [Close]") : $"{text}, {num:F1}m"));
		}
		bool flag = showOnScreenIcons || showLabels;
		string text2 = ((showCompassIcons || showOnScreenIcons) ? GetSprite(entity) : null);
		NavObject val = NavObjectManager.Instance.RegisterNavObject("quest", (Entity)(object)entity, text2, !showCompassIcons);
		if (val != null)
		{
			entityNavObjects[key] = val;
			val.name = name;
			val.usingLocalizationId = false;
			val.hiddenOnCompass = !showCompassIcons;
			val.hiddenOnMap = !showMapIcons;
			val.UseOverrideColor = true;
			val.OverrideColor = ((entity is EntityZombie) ? new Color(1f, 0f, 0f, 0.8f) : (IsHostile(entity) ? new Color(1f, 0.5f, 0f, 0.8f) : new Color(0f, 1f, 0f, 0.8f)));
			if (val.CurrentScreenSettings != null)
			{
				((NavObjectSettings)val.CurrentScreenSettings).MaxDistance = (flag ? DetectionRadius : 0f);
				((NavObjectSettings)val.CurrentScreenSettings).MinDistance = 0f;
				// ShowTextTypes enum was renamed between game versions; use reflection so the port
				// compiles regardless of whether the type is present in the game DLLs.
				try
				{
					var prop = val.CurrentScreenSettings.GetType().GetProperty("ShowTextType");
					if (prop != null)
						prop.SetValue(val.CurrentScreenSettings,
							System.Enum.ToObject(prop.PropertyType, (showLabels && flag) ? 2 : 0));
				}
				catch { }
			}
		}
	}

	private static string GetSprite(EntityAlive e)
	{
		string text = ((Entity)e).EntityClass.entityClassName.ToLowerInvariant();
		if (text.Contains("zombie"))
		{
			return "ui_game_symbol_zombie";
		}
		if (text.Contains("bear"))
		{
			return "ui_game_symbol_tracking_bear";
		}
		if (text.Contains("direwolf") || text.Contains("wolf"))
		{
			return "ui_game_symbol_tracking_wolf";
		}
		if (text.Contains("vulture"))
		{
			return "ui_game_symbol_bat";
		}
		if (e is EntityAnimal)
		{
			return "ui_game_symbol_animal";
		}
		return "ui_game_symbol_enemy";
	}

	private static bool IsHostile(EntityAlive e)
	{
		string text = ((Entity)e).EntityClass.entityClassName.ToLowerInvariant();
		if (!text.Contains("zombie") && !text.Contains("bear") && !text.Contains("direwolf"))
		{
			return text.Contains("vulture");
		}
		return true;
	}

	private static void DisableDetector()
	{
		foreach (NavObject value in entityNavObjects.Values)
		{
			NavObjectManager instance = NavObjectManager.Instance;
			if (instance != null)
			{
				instance.UnRegisterNavObject(value);
			}
		}
		entityNavObjects.Clear();
	}

	private static void Log(string msg)
	{
		if (DebugLogging)
		{
			Debug.Log((object)("[TechFreqs Visual Indicator] " + msg));
		}
	}
}
