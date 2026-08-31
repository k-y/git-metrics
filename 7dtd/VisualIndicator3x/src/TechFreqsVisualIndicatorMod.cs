using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

		public float? UpdateInterval { get; set; } = 1f;

		public bool? DebugLogging { get; set; } = false;

		public bool? ShowDistance { get; set; } = true;

		public bool? ShowLabels { get; set; } = true;

		public bool? ShowCompassIcons { get; set; } = true;

		public bool? ShowOnScreenIcons { get; set; } = true;

		public bool? ShowMapIcons { get; set; } = true;

		public bool? AutoEnable { get; set; } = true;

		public int? FontSize { get; set; } = 12;
	}

	public const string MOD_PREFIX = "[TechFreqs Visual Indicator] ";

	private static string configPath;

	private static DateTime lastConfigWriteTime = DateTime.MinValue;

	private static Mod _modInstance;

	private static readonly Dictionary<string, NavObject> entityNavObjects = new Dictionary<string, NavObject>();

	public static bool IndicatorsEnabled { get; internal set; } = true;

	public static float DetectionRadius { get; private set; } = 50f;

	public static KeyCode ToggleKey { get; private set; } = (KeyCode)59;

	public static float UpdateInterval { get; private set; } = 1f;

	public static float StartDelay { get; private set; } = 5f;

	public static bool DebugLogging { get; private set; } = false;

	public static bool ShowDistance { get; private set; } = true;

	public static bool ShowLabels { get; private set; } = true;

	public static bool ShowCompassIcons { get; private set; } = true;

	public static bool ShowOnScreenIcons { get; private set; } = true;

	public static bool ShowMapIcons { get; private set; } = true;

	public static bool AutoEnable { get; private set; } = true;

	public static int FontSize { get; private set; } = 12;

	public void InitMod(Mod modInstance)
	{
		_modInstance = modInstance;
		configPath = Path.Combine(modInstance.Path, "config.json");
		Log("<color=cyan>TechFreqs Visual Indicator v3.0 LOADED</color>");
		LoadConfig();
		IndicatorsEnabled = AutoEnable;
		((MonoBehaviour)GameManager.Instance).StartCoroutine(MainLoop());
		((MonoBehaviour)GameManager.Instance).StartCoroutine(InputLoop());
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
				if (val != null)
				{
					try { UpdateEntityDetector(val); }
					catch (Exception ex) { Log("UpdateEntityDetector error: " + ex.Message); }
				}
			}
			yield return (object)new WaitForSeconds(UpdateInterval);
		}
	}

	private static IEnumerator InputLoop()
	{
		while (true)
		{
			if (Input.GetKeyDown(ToggleKey))
			{
				var world = GameManager.Instance?.World;
				var player = world != null ? ((WorldBase)world).GetPrimaryPlayer() : null;
				if (player != null && !((Entity)player).isEntityRemote && ((Entity)player).IsSpawned())
				{
					IndicatorsEnabled = !IndicatorsEnabled;
					GameManager.ShowTooltip(player, MOD_PREFIX +
						(IndicatorsEnabled ? "ENABLED" : "DISABLED"), false, false, 0f);
					if (!IndicatorsEnabled) DisableDetector();
				}
			}
			yield return null;
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
				DisableDetector();
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
				if (val != null)
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
			FontSize = Mathf.Clamp(config.FontSize ?? 12, 6, 48);
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
		File.WriteAllText(configPath, JsonConvert.SerializeObject((object)config, Newtonsoft.Json.Formatting.Indented));
		Log("Default config.json created");
	}

	private static void UpdateEntityDetector(EntityPlayerLocal player)
	{
		if (player == null || ((Entity)player).world?.Entities?.dict == null || NavObjectManager.Instance == null)
			return;

		var activeKeys = new HashSet<string>();

		foreach (KeyValuePair<int, Entity> item in ((Entity)player).world.Entities.dict)
		{
			Entity value = item.Value;
			if (value == null || value.entityId == ((Entity)player).entityId || value.IsDespawned) continue;
			if (Vector3.Distance(((Entity)player).position, value.position) > DetectionRadius) continue;

			string containerLabel = GetContainerLabel(value);
			if (containerLabel != null)
			{
				string key = $"container_{value.entityId}";
				activeKeys.Add(key);
				CreateOrUpdateContainerNavObject(player, key, value, containerLabel);
			}
		}

		var keysToRemove = new List<string>();
		foreach (string key in entityNavObjects.Keys)
			if (!activeKeys.Contains(key)) keysToRemove.Add(key);
		foreach (string key in keysToRemove)
		{
			NavObjectManager.Instance.UnRegisterNavObject(entityNavObjects[key]);
			entityNavObjects.Remove(key);
		}
	}

	private static string GetContainerLabel(Entity entity)
	{
		string cn = entity.EntityClass?.entityClassName;
		if (string.IsNullOrEmpty(cn)) return null;
		string cnl = cn.ToLowerInvariant();
		if (cn == "BossLootContainerCarrier")                                    return "chest";
		if (cnl.StartsWith("bosslootcontainer"))                                 return "box";
		if (cn == "MiniBossLootContainer")                                       return "mini";
		if (cn == "ChargedEliteLootContainer" || cn == "InfernalEliteLootContainer") return "red";
		if (cnl.Contains("smallminiboss"))                                       return "red";
		if (cnl.StartsWith("entitylootcontainer"))
		{
			if (cnl.Contains("strong")) return "blu";
			if (cnl.Contains("plague")) return "org";
			if (cnl.Contains("boss"))   return "red";
			return "yel";
		}
		return null;
	}

	private static Color GetContainerColor(string label) => label switch
	{
		"yel"   => new Color(1f,   0.9f,  0f,   0.8f),
		"blu"   => new Color(0f,   0.4f,  1f,   1f),
		"org"   => new Color(1f,   0.55f, 0f,   0.8f),
		"red"   => new Color(1f,   0.1f,  0.1f, 0.8f),
		"mini"  => new Color(0.9f, 0f,    0.9f, 0.8f),
		"box"   => new Color(1f,   0.3f,  0.7f, 0.8f),
		"chest" => new Color(1f,   0.85f, 0f,   0.8f),
		_       => new Color(1f,   1f,    1f,   0.8f),
	};

	private static void CreateOrUpdateContainerNavObject(EntityPlayerLocal player, string key, Entity entity, string label)
	{
		float dist = Vector3.Distance(((Entity)player).position, entity.position);
		string name = "";
		if (ShowLabels)
			name = ShowDistance ? $"{label} {dist:F0}m" : label;

		if (entityNavObjects.TryGetValue(key, out NavObject val) && val != null)
		{
			val.name = name;
			return;
		}

		bool flag = ShowOnScreenIcons || ShowLabels;
		try
		{
			val = NavObjectManager.Instance.RegisterNavObject("TFVIcontainer", entity, "ui_game_symbol_loot_sack", false);
			if (val == null)
				val = NavObjectManager.Instance.RegisterNavObject("quest", entity, "ui_game_symbol_loot_sack", false);
		}
		catch (Exception ex) { Log("RegisterNavObject container error: " + ex.Message); val = null; }
		if (val == null) return;
		entityNavObjects[key] = val;
		val.name = name;
		val.usingLocalizationId = false;
		val.hiddenOnCompass = false;
		val.hiddenOnMap = true;
		val.UseOverrideColor = true;
		val.OverrideColor = GetContainerColor(label);
		if (val.CurrentScreenSettings is NavObjectScreenSettings screen)
		{
			screen.MaxDistance = flag ? DetectionRadius : 0f;
			screen.MinDistance = 0f;
			screen.ShowTextType = (ShowLabels && flag)
				? NavObjectScreenSettings.ShowTextTypes.Name
				: NavObjectScreenSettings.ShowTextTypes.None;
			screen.FontSize = FontSize;
		}
	}

	internal static void DisableDetectorPublic() => DisableDetector();

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
