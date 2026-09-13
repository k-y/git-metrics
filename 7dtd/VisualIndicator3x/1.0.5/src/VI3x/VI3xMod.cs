using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VI3xMod;

public class VI3xMod : IModApi
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

	public const string MOD_PREFIX = "[VI3x] ";

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
		Log("<color=cyan>VI3x v3.0 LOADED</color>");
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
				if ((Object)(object)instance == (Object)null)
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

	private static IEnumerator InputLoop()
	{
		while (true)
		{
			if (Input.GetKeyDown(ToggleKey))
			{
				GameManager instance = GameManager.Instance;
				World world = ((instance != null) ? instance.World : null);
				EntityPlayerLocal player = ((world != null) ? ((WorldBase)world).GetPrimaryPlayer() : null);
				if ((Object)(object)player != (Object)null && !((Entity)player).isEntityRemote && ((Entity)player).IsSpawned())
				{
					IndicatorsEnabled = !IndicatorsEnabled;
					GameManager.ShowTooltip(player, "[VI3x] " + (IndicatorsEnabled ? "ENABLED" : "DISABLED"), false, false, 0f);
					if (!IndicatorsEnabled)
					{
						DisableDetector();
					}
				}
			}
			yield return null;
		}
	}

	private static void CheckForConfigChangesAndReload()
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
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
				if ((Object)(object)instance == (Object)null)
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
					GameManager.ShowTooltip(val, "[VI3x] Config Reloaded!", false, false, 0f);
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
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
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
		File.WriteAllText(configPath, JsonConvert.SerializeObject((object)config, (Formatting)1));
		Log("Default config.json created");
	}

	private static void UpdateEntityDetector(EntityPlayerLocal player)
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)player == (Object)null || ((Entity)player).world?.Entities?.dict == null || NavObjectManager.Instance == null)
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
			if ((Object)(object)value == (Object)null || value.entityId == ((Entity)player).entityId || value.IsDespawned || Vector3.Distance(((Entity)player).position, value.position) > DetectionRadius)
			{
				continue;
			}
			string containerLabel = GetContainerLabel(value);
			if (containerLabel != null)
			{
				string key = $"container_{value.entityId}";
				CreateOrUpdateContainerNavObject(player, key, value, containerLabel);
				continue;
			}
			EntityAlive val = (EntityAlive)(object)((value is EntityAlive) ? value : null);
			if ((Object)(object)val != (Object)null && ((Entity)val).IsAlive())
			{
				string key2 = $"entity_{value.entityId}";
				CreateOrUpdateNavObject(player, key2, val);
			}
		}
	}

	private static void CreateOrUpdateNavObject(EntityPlayerLocal player, string key, EntityAlive entity)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		float num = Vector3.Distance(((Entity)player).position, ((Entity)entity).position);
		bool showCompassIcons = ShowCompassIcons;
		bool showOnScreenIcons = ShowOnScreenIcons;
		bool showMapIcons = ShowMapIcons;
		bool showLabels = ShowLabels;
		bool showDistance = ShowDistance;
		string name = "";
		if (showLabels)
		{
			string text = BuildShortLabel(entity);
			if (!string.IsNullOrEmpty(text))
			{
				name = (showDistance ? $"{text} {num:F0}m" : text);
			}
		}
		bool flag = showOnScreenIcons || showLabels;
		NavObject val = NavObjectManager.Instance.RegisterNavObject("quest", (Entity)entity, GetSprite(entity), !showCompassIcons);
		if (val != null)
		{
			entityNavObjects[key] = val;
			val.name = name;
			val.usingLocalizationId = false;
			val.hiddenOnCompass = !showCompassIcons;
			val.hiddenOnMap = !showMapIcons;
			val.UseOverrideColor = true;
			val.OverrideColor = ((entity is EntityZombie) ? new Color(1f, 0f, 0f, 0.8f) : (IsHostile(entity) ? new Color(1f, 0.5f, 0f, 0.8f) : new Color(0f, 1f, 0f, 0.8f)));
			NavObjectScreenSettings currentScreenSettings = val.CurrentScreenSettings;
			if (currentScreenSettings != null)
			{
				((NavObjectSettings)currentScreenSettings).MaxDistance = (flag ? DetectionRadius : 0f);
				((NavObjectSettings)currentScreenSettings).MinDistance = 0f;
				currentScreenSettings.ShowTextType = (NavObjectScreenSettings.ShowTextTypes)((showLabels && flag) ? 2 : 0);
				currentScreenSettings.FontSize = FontSize;
			}
		}
	}

	private static string GetNavObjectClass(EntityAlive e)
	{
		string text = ((Entity)e).EntityClass.entityClassName.ToLowerInvariant();
		if (text.Contains("zombie") || text.Contains("boss") || text.Contains("feral") || text.Contains("radiated") || text.Contains("elite"))
		{
			if (text.Contains("boss"))
			{
				return "EnemyBoss";
			}
			if (text.Contains("mini"))
			{
				return "EnemyMiniboss";
			}
			if (text.Contains("radiated"))
			{
				return "EnemyRadiated";
			}
			if (text.Contains("feral"))
			{
				return "EnemyFeral";
			}
			if (text.Contains("elite"))
			{
				return "EnemyElite";
			}
			return "EnemyZombie";
		}
		if (text.Contains("vulture"))
		{
			return "EnemyVulture";
		}
		if (e is EntityAnimal)
		{
			if (text.Contains("bear"))
			{
				return "animaltracking_bear";
			}
			if (text.Contains("direwolf"))
			{
				return "animaltracking_direwolf";
			}
			if (text.Contains("wolf"))
			{
				return "animaltracking_wolf";
			}
			if (text.Contains("mountainlion") || text.Contains("lion"))
			{
				return "animaltracking_mountainlion";
			}
			if (text.Contains("snake"))
			{
				return "animaltracking_snake";
			}
			if (text.Contains("coyote"))
			{
				return "animaltracking_coyote";
			}
			if (text.Contains("boar"))
			{
				return "animaltracking_boar";
			}
			if (text.Contains("stag"))
			{
				return "animaltracking_stag";
			}
			if (text.Contains("doe"))
			{
				return "animaltracking_doe";
			}
			if (text.Contains("rabbit"))
			{
				return "animaltracking_rabbit";
			}
			if (text.Contains("chicken"))
			{
				return "animaltracking_chicken";
			}
			return "animaltracking_timid";
		}
		return "EnemyDot";
	}

	private static string BuildShortLabel(EntityAlive entity)
	{
		string text = ((Entity)entity).EntityClass?.entityClassName?.ToLowerInvariant() ?? "";
		if (text.Contains("boss"))
		{
			return "BOSS";
		}
		if (text.Contains("zombie"))
		{
			string text2 = ((Entity)entity).EntityClass?.entityClassName ?? "";
			int num = text2.IndexOf("zombie", StringComparison.OrdinalIgnoreCase);
			string text3 = ((num >= 0) ? text2.Substring(num + 6).TrimStart('_') : "");
			if (text3.StartsWith("Male", StringComparison.OrdinalIgnoreCase))
			{
				text3 = text3.Substring(4).TrimStart('_');
			}
			else if (text3.StartsWith("Female", StringComparison.OrdinalIgnoreCase))
			{
				text3 = text3.Substring(6).TrimStart('_');
			}
			return string.IsNullOrEmpty(text3) ? "Z" : ("Z " + text3);
		}
		if (text.Contains("trader"))
		{
			return "Trader";
		}
		if (text.Contains("drone"))
		{
			return "Drone";
		}
		if (entity is EntityAnimal || text.Contains("snake") || text.Contains("vulture"))
		{
			if (text.Contains("bear"))
			{
				return "Bear";
			}
			if (text.Contains("direwolf"))
			{
				return "Dire";
			}
			if (text.Contains("wolf"))
			{
				return "Wolf";
			}
			if (text.Contains("mountainlion") || text.Contains("lion"))
			{
				return "Lion";
			}
			if (text.Contains("boar"))
			{
				return "Boar";
			}
			if (text.Contains("coyote"))
			{
				return "Coyote";
			}
			if (text.Contains("snake"))
			{
				return "Snake";
			}
			if (text.Contains("vulture"))
			{
				return "Vulture";
			}
			if (text.Contains("stag"))
			{
				return "Stag";
			}
			if (text.Contains("doe"))
			{
				return "Doe";
			}
			if (text.Contains("rabbit"))
			{
				return "Rabbit";
			}
			if (text.Contains("chicken"))
			{
				return "Chicken";
			}
			return "";
		}
		string debugName = ((Entity)entity).GetDebugName();
		return string.IsNullOrEmpty(debugName) ? text : debugName;
	}

	private static string GetSprite(EntityAlive e)
	{
		string text = ((Entity)e).EntityClass.entityClassName.ToLowerInvariant();
		if (e is EntityAnimal || text.Contains("snake") || text.Contains("vulture"))
		{
			if (text.Contains("bear"))
			{
				return "ui_game_symbol_tracking_bear";
			}
			if (text.Contains("direwolf"))
			{
				return "ui_game_symbol_tracking_direwolf";
			}
			if (text.Contains("wolf"))
			{
				return "ui_game_symbol_tracking_wolf";
			}
			if (text.Contains("mountainlion") || text.Contains("lion"))
			{
				return "ui_game_symbol_tracking_mountainlion";
			}
			if (text.Contains("boar"))
			{
				return "ui_game_symbol_tracking_boar";
			}
			if (text.Contains("coyote"))
			{
				return "ui_game_symbol_tracking_coyote";
			}
			if (text.Contains("snake"))
			{
				return "ui_game_symbol_tracking_snake";
			}
			if (text.Contains("stag"))
			{
				return "ui_game_symbol_tracking_stag";
			}
			if (text.Contains("doe"))
			{
				return "ui_game_symbol_tracking_doe";
			}
			if (text.Contains("rabbit"))
			{
				return "ui_game_symbol_tracking_rabbit";
			}
			if (text.Contains("chicken"))
			{
				return "ui_game_symbol_tracking_chicken";
			}
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
		string text = entity.EntityClass?.entityClassName;
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		string text2 = text.ToLowerInvariant();
		if (text == "BossLootContainerCarrier")
		{
			return "chest";
		}
		if (text2.StartsWith("bosslootcontainer"))
		{
			return "box";
		}
		if (text == "MiniBossLootContainer")
		{
			return "mini";
		}
		if (text == "ChargedEliteLootContainer" || text == "InfernalEliteLootContainer")
		{
			return "red";
		}
		if (text2.Contains("smallminiboss"))
		{
			return "red";
		}
		if (text2.StartsWith("entitylootcontainer"))
		{
			if (text2.Contains("strong"))
			{
				return "blu";
			}
			if (text2.Contains("plague"))
			{
				return "org";
			}
			if (text2.Contains("boss"))
			{
				return "red";
			}
			return "yel";
		}
		if (text2.StartsWith("ns") && text2.Contains("mobbag"))
		{
			return "yel";
		}
		if (text2.StartsWith("ns") && !text2.Contains("mobbag"))
		{
			// Gold (zpackGoldPrefab): EX boss series, BornLootBag, MusubiRareBag
			if (text2 == "nsbossbornlootbag" || text2 == "nsmusubirarebag" ||
			    (text2.StartsWith("nsboss") && text2.Contains("ex") && text2.EndsWith("lootbag") && !text2.Contains("kasper")))
				return "gld";
			// Blue (zpackBluePrefab): echo, mid, supply, shukuen
			if (text2.StartsWith("nsecho") || text2.StartsWith("nsmid") ||
			    text2 == "nsmusubisupplybag" || text2 == "nsshukuenbag")
				return "blu";
			// Special non-backpack containers: weapon bag, mag/time/boom crates
			if (text2 == "nsmusubiweaponbag" || text2 == "nsmagbag" ||
			    text2 == "nstimebag" || text2 == "nsboombag")
				return "box";
			// Red (zpackRedPrefab inherited): standard boss bags, Kasper, wild boss, misc
			if (text2.StartsWith("nsboss") || text2.StartsWith("nswildboss") ||
			    text2 == "nsmusubinigibag" || text2 == "nsshachikubag" ||
			    text2 == "nsredcometbag" || text2 == "nsmbag")
				return "red";
		}
		return null;
	}

	private static Color GetContainerColor(string label)
	{
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		if (1 == 0)
		{
		}
		Color result = (Color)(label switch
		{
			"yel"   => new Color(1f,    0.9f,  0f,    0.8f),
			"blu"   => new Color(0f,    0.4f,  1f,    1f),
			"org"   => new Color(1f,    0.55f, 0f,    0.8f),
			"red"   => new Color(1f,    0.1f,  0.1f,  0.8f),
			"mini"  => new Color(0.9f,  0f,    0.9f,  0.8f),
			"box"   => new Color(1f,    0.3f,  0.7f,  0.8f),
			"chest" => new Color(1f,    0.85f, 0f,    0.8f),
			"gld"   => new Color(1f,    0.75f, 0f,    0.9f),
			_       => new Color(1f,    1f,    1f,    0.8f),
		});
		if (1 == 0)
		{
		}
		return result;
	}

	private static void CreateOrUpdateContainerNavObject(EntityPlayerLocal player, string key, Entity entity, string label)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		float num = Vector3.Distance(((Entity)player).position, entity.position);
		bool flag = ShowOnScreenIcons || ShowLabels;
		string name = "";
		if (ShowLabels)
		{
			name = (ShowDistance ? $"{label} {num:F0}m" : label);
		}
		NavObject val = NavObjectManager.Instance.RegisterNavObject("quest", entity, "ui_game_symbol_loot_sack", true);
		if (val != null)
		{
			entityNavObjects[key] = val;
			val.name = name;
			val.usingLocalizationId = false;
			val.hiddenOnCompass = false;
			val.hiddenOnMap = true;
			val.UseOverrideColor = true;
			val.OverrideColor = GetContainerColor(label);
			NavObjectScreenSettings currentScreenSettings = val.CurrentScreenSettings;
			if (currentScreenSettings != null)
			{
				((NavObjectSettings)currentScreenSettings).MaxDistance = (flag ? DetectionRadius : 0f);
				((NavObjectSettings)currentScreenSettings).MinDistance = 0f;
				currentScreenSettings.ShowTextType = (NavObjectScreenSettings.ShowTextTypes)((ShowLabels && flag) ? 2 : 0);
				currentScreenSettings.FontSize = FontSize;
			}
		}
	}

	internal static void DisableDetectorPublic()
	{
		DisableDetector();
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
			Debug.Log((object)("[VI3x] " + msg));
		}
	}
}
