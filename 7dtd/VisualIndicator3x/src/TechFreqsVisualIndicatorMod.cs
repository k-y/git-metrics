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

			// Container check first — EntityLootContainer may extend EntityAlive,
			// so match by class name before IsAlive() would silently skip it.
			string containerLabel = GetContainerLabel(value);
			if (containerLabel != null)
			{
				string key = $"container_{value.entityId}";
				activeKeys.Add(key);
				CreateOrUpdateContainerNavObject(player, key, value, containerLabel);
				continue;
			}

			EntityAlive alive = value as EntityAlive;
			if (alive != null && ((Entity)alive).IsAlive())
			{
				string key = $"entity_{value.entityId}";
				activeKeys.Add(key);
				CreateOrUpdateNavObject(player, key, alive);
			}
		}

		// Remove nav objects for entities that left range or despawned
		var keysToRemove = new List<string>();
		foreach (string key in entityNavObjects.Keys)
			if (!activeKeys.Contains(key)) keysToRemove.Add(key);
		foreach (string key in keysToRemove)
		{
			NavObjectManager.Instance.UnRegisterNavObject(entityNavObjects[key]);
			entityNavObjects.Remove(key);
		}
	}

	private static void CreateOrUpdateNavObject(EntityPlayerLocal player, string key, EntityAlive entity)
	{
		float num = Vector3.Distance(((Entity)player).position, ((Entity)entity).position);
		bool showLabels = ShowLabels;
		bool showDistance = ShowDistance;
		string name = "";
		if (showLabels)
		{
			string label = BuildShortLabel(entity);
			if (!string.IsNullOrEmpty(label))
				name = showDistance ? $"{label} {num:F0}m" : label;
		}

		if (entityNavObjects.TryGetValue(key, out NavObject val) && val != null)
		{
			val.name = name;
			return;
		}

		bool showCompassIcons = ShowCompassIcons;
		bool showOnScreenIcons = ShowOnScreenIcons;
		bool showMapIcons = ShowMapIcons;
		bool flag = showOnScreenIcons || showLabels;
		try
		{
			val = NavObjectManager.Instance.RegisterNavObject(GetNavObjectClass(entity), (Entity)(object)entity, GetSprite(entity), !showCompassIcons);
			if (val == null)
				val = NavObjectManager.Instance.RegisterNavObject("quest", (Entity)(object)entity, GetSprite(entity), !showCompassIcons);
		}
		catch (Exception ex) { Log("RegisterNavObject entity error: " + ex.Message); val = null; }
		if (val == null) return;
		entityNavObjects[key] = val;
		val.name = name;
		val.usingLocalizationId = false;
		val.hiddenOnCompass = !showCompassIcons;
		val.hiddenOnMap = !showMapIcons;
		val.UseOverrideColor = true;
		val.OverrideColor = (entity is EntityZombie) ? new Color(1f, 0f, 0f, 0.8f) : (IsHostile(entity) ? new Color(1f, 0.5f, 0f, 0.8f) : new Color(0f, 1f, 0f, 0.8f));
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

	private static string GetNavObjectClass(EntityAlive e)
	{
		string cn = ((Entity)e).EntityClass?.entityClassName?.ToLowerInvariant() ?? "";
		if (cn.Contains("boss") && cn.Contains("mini"))              return "TFVIminiboss";
		if (cn.Contains("boss"))                                     return "TFVIboss";
		if (cn.Contains("radiated"))                                 return "TFVIradiated";
		if (cn.Contains("feral"))                                    return "TFVIferal";
		if (cn.Contains("elite"))                                    return "TFVIelite";
		if (cn.Contains("zombie") || e is EntityZombie)             return "TFVIzombie";
		if (cn.Contains("vulture"))                                  return "TFVIanimal_vulture";
		if (e is EntityAnimal || cn.Contains("snake"))
		{
			if (cn.Contains("bear"))                                     return "TFVIanimal_bear";
			if (cn.Contains("direwolf"))                                 return "TFVIanimal_direwolf";
			if (cn.Contains("wolf"))                                     return "TFVIanimal_wolf";
			if (cn.Contains("mountainlion") || cn.Contains("lion"))      return "TFVIanimal_mountainlion";
			if (cn.Contains("boar"))                                     return "TFVIanimal_boar";
			if (cn.Contains("snake"))                                    return "TFVIanimal_snake";
			if (cn.Contains("coyote"))                                   return "TFVIanimal_coyote";
			if (cn.Contains("dog"))                                      return "TFVIanimal_dog";
			if (cn.Contains("stag"))                                     return "TFVIanimal_stag";
			if (cn.Contains("doe") || cn.Contains("deer"))               return "TFVIanimal_deer";
			if (cn.Contains("rabbit"))                                   return "TFVIanimal_rabbit";
			if (cn.Contains("chicken"))                                  return "TFVIanimal_chicken";
			return "TFVIanimal_timid";
		}
		return "TFVIzombie";
	}

	private static string BuildShortLabel(EntityAlive entity)
	{
		string cn = ((Entity)entity).EntityClass?.entityClassName?.ToLowerInvariant() ?? "";
		if (cn.Contains("boss"))    return "BOSS";
		if (cn.Contains("zombie"))
		{
			string orig = ((Entity)entity).EntityClass?.entityClassName ?? "";
			int idx = orig.IndexOf("zombie", StringComparison.OrdinalIgnoreCase);
			string suffix = idx >= 0 ? orig.Substring(idx + 6).TrimStart('_') : "";
			if (suffix.StartsWith("Male", StringComparison.OrdinalIgnoreCase))
				suffix = suffix.Substring(4).TrimStart('_');
			else if (suffix.StartsWith("Female", StringComparison.OrdinalIgnoreCase))
				suffix = suffix.Substring(6).TrimStart('_');
			return string.IsNullOrEmpty(suffix) ? "Z" : "Z " + suffix;
		}
		if (cn.Contains("trader"))  return "Trader";
		if (cn.Contains("drone"))   return "Drone";
		if (entity is EntityAnimal || cn.Contains("snake") || cn.Contains("vulture"))
		{
			if (cn.Contains("bear"))                                 return "Bear";
			if (cn.Contains("direwolf"))                             return "Dire";
			if (cn.Contains("wolf"))                                 return "Wolf";
			if (cn.Contains("mountainlion") || cn.Contains("lion")) return "Lion";
			if (cn.Contains("boar"))                                 return "Boar";
			if (cn.Contains("coyote"))                               return "Coyote";
			if (cn.Contains("snake"))                                return "Snake";
			if (cn.Contains("vulture"))                              return "Vulture";
			if (cn.Contains("stag"))                                 return "Stag";
			if (cn.Contains("doe"))                                  return "Doe";
			if (cn.Contains("rabbit"))                               return "Rabbit";
			if (cn.Contains("chicken"))                              return "Chicken";
			return "";
		}
		string debugName = ((Entity)entity).GetDebugName();
		return string.IsNullOrEmpty(debugName) ? cn : debugName;
	}

	private static string GetSprite(EntityAlive e)
	{
		string cn = ((Entity)e).EntityClass.entityClassName.ToLowerInvariant();
		if (e is EntityAnimal || cn.Contains("snake") || cn.Contains("vulture"))
		{
			if (cn.Contains("bear"))                                 return "ui_game_symbol_tracking_bear";
			if (cn.Contains("direwolf"))                             return "ui_game_symbol_tracking_direwolf";
			if (cn.Contains("wolf"))                                 return "ui_game_symbol_tracking_wolf";
			if (cn.Contains("mountainlion") || cn.Contains("lion")) return "ui_game_symbol_tracking_mountainlion";
			if (cn.Contains("boar"))                                 return "ui_game_symbol_tracking_boar";
			if (cn.Contains("coyote"))                               return "ui_game_symbol_tracking_coyote";
			if (cn.Contains("snake"))                                return "ui_game_symbol_tracking_snake";
			if (cn.Contains("stag"))                                 return "ui_game_symbol_tracking_stag";
			if (cn.Contains("doe"))                                  return "ui_game_symbol_tracking_doe";
			if (cn.Contains("rabbit"))                               return "ui_game_symbol_tracking_rabbit";
			if (cn.Contains("chicken"))                              return "ui_game_symbol_tracking_chicken";
			return "ui_game_symbol_tracking_timid";
		}
		return "ui_game_symbol_tracking_zombie";
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
