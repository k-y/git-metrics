using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace TechFreqsVisualIndicatorMod;

// Handles key toggle in Unity's Update loop — no Harmony dependency needed.
internal class ToggleBehaviour : MonoBehaviour
{
	void Awake()
	{
		Debug.Log("[TechFreqs Visual Indicator] ToggleBehaviour Awake — Update loop starting");
	}

	void Update()
	{
		if (Input.anyKeyDown)
			Debug.Log("[TechFreqs Visual Indicator] Update running — key: " + TechFreqsVisualIndicatorMod.ToggleKey);
		if (!Input.GetKeyDown(TechFreqsVisualIndicatorMod.ToggleKey)) return;
		Debug.Log("[TechFreqs Visual Indicator] Toggle key detected: " + TechFreqsVisualIndicatorMod.ToggleKey);
		var world = GameManager.Instance?.World;
		var player = world != null ? ((WorldBase)world).GetPrimaryPlayer() : null;
		if (player == null)           { Debug.Log("[TechFreqs Visual Indicator] Toggle blocked: no player");      return; }
		if (((Entity)player).isEntityRemote) { Debug.Log("[TechFreqs Visual Indicator] Toggle blocked: isEntityRemote"); return; }
		if (!((Entity)player).IsSpawned())   { Debug.Log("[TechFreqs Visual Indicator] Toggle blocked: not spawned");    return; }
		TechFreqsVisualIndicatorMod.IndicatorsEnabled = !TechFreqsVisualIndicatorMod.IndicatorsEnabled;
		Debug.Log("[TechFreqs Visual Indicator] Toggled to: " + TechFreqsVisualIndicatorMod.IndicatorsEnabled);
		GameManager.ShowTooltip(player, "[TechFreqs Visual Indicator] " +
			(TechFreqsVisualIndicatorMod.IndicatorsEnabled ? "ENABLED" : "DISABLED"), false, false, 0f);
		if (!TechFreqsVisualIndicatorMod.IndicatorsEnabled)
			TechFreqsVisualIndicatorMod.DisableDetectorPublic();
	}
}

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

	public static float UpdateInterval { get; private set; } = 3f;

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
		var go = new GameObject("TechFreqsVisualIndicator");
		UnityEngine.Object.DontDestroyOnLoad(go);
		go.AddComponent<ToggleBehaviour>();
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
				if (val != null)
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
		foreach (NavObject value2 in entityNavObjects.Values)
			NavObjectManager.Instance.UnRegisterNavObject(value2);
		entityNavObjects.Clear();
		foreach (KeyValuePair<int, Entity> item in ((Entity)player).world.Entities.dict)
		{
			Entity value = item.Value;
			if (value == null || value.entityId == ((Entity)player).entityId || value.IsDespawned) continue;
			if (Vector3.Distance(((Entity)player).position, value.position) > DetectionRadius) continue;

			EntityAlive alive = value as EntityAlive;
			if (alive != null)
			{
				if (((Entity)alive).IsAlive())
				{
					string key = $"entity_{value.entityId}";
					CreateOrUpdateNavObject(player, key, alive);
				}
				continue;
			}

			string containerLabel = GetContainerLabel(value);
			if (containerLabel != null)
			{
				string key = $"container_{value.entityId}";
				CreateOrUpdateContainerNavObject(player, key, value, containerLabel);
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
			string label = BuildShortLabel(entity);
			if (!string.IsNullOrEmpty(label))
				name = showDistance ? $"{label} {num:F0}m" : label;
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
			if (val.CurrentScreenSettings is NavObjectScreenSettings screen)
			{
				screen.MaxDistance = flag ? DetectionRadius : 0f;
				screen.MinDistance = 0f;
				screen.ShowTextType = (showLabels && flag)
					? NavObjectScreenSettings.ShowTextTypes.Name
					: NavObjectScreenSettings.ShowTextTypes.None;
				screen.FontSize = FontSize;
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

	private static string BuildShortLabel(EntityAlive entity)
	{
		string cn = ((Entity)entity).EntityClass?.entityClassName?.ToLowerInvariant() ?? "";
		if (cn.Contains("boss"))    return "BOSS";   // before zombie — boss names often contain "zombie"
		if (cn.Contains("zombie"))  return "Z";
		if (cn.Contains("trader"))  return "Trader";
		if (cn.Contains("drone"))   return "Drone";
		if (entity is EntityAnimal) return "";        // icon only, no text
		// players: use their actual name
		string debugName = ((Entity)entity).GetDebugName();
		return string.IsNullOrEmpty(debugName) ? cn : debugName;
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
		"blu"   => new Color(0.3f, 0.5f,  1f,   0.8f),
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
		bool flag = ShowOnScreenIcons || ShowLabels;
		string name = "";
		if (ShowLabels)
			name = ShowDistance ? $"{label} {dist:F0}m" : label;
		NavObject val = NavObjectManager.Instance.RegisterNavObject("quest", entity, "ui_game_symbol_loot", !ShowCompassIcons);
		if (val != null)
		{
			entityNavObjects[key] = val;
			val.name = name;
			val.usingLocalizationId = false;
			val.hiddenOnCompass = true;
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
