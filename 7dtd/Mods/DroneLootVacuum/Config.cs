using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

namespace DroneLootVacuum;

public static class Config
{
	public static bool Enabled = true;

	public static bool DebugLog = false;

	public static float Radius = 8f;

	public static float ScanInterval = 0.5f;

	public static bool ZombieBagsOnly = true;

	public static bool VacuumWorldContainers = true;

	public static bool OnlyLootUntouchedContainers = true;

	public static bool RemoveEmptyContainers = true;

	public static string[] RemoveContainerNames = new string[17]
	{
		"trashpile", "birdnest", "foodpile", "sportsbag", "backpack", "medicloot", "medical supplies", "ammopile", "chempile", "chemistry set",
		"clothespile", "liquorpile", "garment bag", "duffle bag", "luggage", "suitcase", "bookpile"
	};

	public static string[] DowngradeContainerNames = new string[26]
	{
		"mailbox", "shelf", "shelves", "charcoalgrill", "gasgrill", "cooler", "stove", "fridge", "refrigerator", "medicinecabinet",
		"medical cabinet", "bintrashmetal", "bintrashplastic", "metal trash bin", "washer", "washing machine", "dryer", "deskmetal", "deskwood", "metal desk",
		"filecabinet", "shoppingcart", "locker", "rollingtoolbox", "janitorcart", "tilttruck"
	};

	public static bool PickupAmmo = true;

	public static bool PickupWeapons = true;

	public static bool PickupTools = true;

	public static bool PickupArmor = true;

	public static bool PickupClothing = true;

	public static bool PickupMedical = true;

	public static bool PickupFood = true;

	public static bool PickupDrinks = true;

	public static bool PickupBooks = true;

	public static bool PickupMods = true;

	public static bool PickupSeeds = true;

	public static bool PickupOres = true;

	public static bool PickupResources = true;

	public static bool PickupBuilding = true;

	public static bool PickupChemicals = true;

	public static bool PickupMisc = true;

	public static bool PickupReadBooks = true;

	public static bool SortLootEnabled = true;

	public static Color SortIconColor = new Color(0.39f, 0.86f, 0.47f, 1f);

	public static bool AmmoRunnerEnabled = true;

	public static int AmmoRunnerThreshold = 10;

	public static int AmmoRunnerAmount = 60;

	public static float ResupplyCooldown = 5f;

	public static bool MedicRunnerEnabled = true;

	public static string[] MedicItems = new string[3] { "medicalFirstAidBandage", "medicalFirstAidKit", "medicalBandage" };

	public static int MedicRunnerThreshold = 1;

	public static int MedicRunnerAmount = 2;

	public static Color VacuumOnColor = new Color(0.39f, 0.86f, 0.47f, 1f);

	public static Color VacuumOffColor = new Color(1f, 0.38f, 0.38f, 1f);

	public static bool DepositEnabled = true;

	public static string DepositLabel = "Drone";

	public static float DepositRadius = 8f;

	public static bool DepositWhenFull = false;

	public static bool RecallEnabled = true;

	public static KeyCode RecallKey = (KeyCode)110;

	public static void Load(string modPath)
	{
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0470: Unknown result type (might be due to invalid IL or missing references)
		//IL_0472: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = Path.Combine(modPath ?? ".", "Config", "dronevacuum.xml");
			if (!File.Exists(text))
			{
				Log("no config at " + text + "; using defaults");
				return;
			}
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(text);
			Enabled = GetBool(xmlDocument, "enabled", Enabled);
			DebugLog = GetBool(xmlDocument, "debug", DebugLog);
			Radius = GetFloat(xmlDocument, "pickup_radius", Radius);
			ScanInterval = GetFloat(xmlDocument, "scan_interval", ScanInterval);
			ZombieBagsOnly = GetBool(xmlDocument, "zombie_bags_only", ZombieBagsOnly);
			VacuumWorldContainers = GetBool(xmlDocument, "vacuum_world_containers", VacuumWorldContainers);
			OnlyLootUntouchedContainers = GetBool(xmlDocument, "only_loot_untouched_containers", OnlyLootUntouchedContainers);
			RemoveEmptyContainers = GetBool(xmlDocument, "remove_empty_containers", RemoveEmptyContainers);
			RemoveContainerNames = GetNames(xmlDocument, "remove_container_names", RemoveContainerNames);
			DowngradeContainerNames = GetNames(xmlDocument, "downgrade_container_names", DowngradeContainerNames);
			PickupAmmo = GetBool(xmlDocument, "pickup_ammo", PickupAmmo);
			PickupWeapons = GetBool(xmlDocument, "pickup_weapons", PickupWeapons);
			PickupTools = GetBool(xmlDocument, "pickup_tools", PickupTools);
			PickupArmor = GetBool(xmlDocument, "pickup_armor", PickupArmor);
			PickupClothing = GetBool(xmlDocument, "pickup_clothing", PickupClothing);
			PickupMedical = GetBool(xmlDocument, "pickup_medical", PickupMedical);
			PickupFood = GetBool(xmlDocument, "pickup_food", PickupFood);
			PickupDrinks = GetBool(xmlDocument, "pickup_drinks", PickupDrinks);
			PickupBooks = GetBool(xmlDocument, "pickup_books", PickupBooks);
			PickupReadBooks = GetBool(xmlDocument, "pickup_read_books", PickupReadBooks);
			PickupMods = GetBool(xmlDocument, "pickup_mods", PickupMods);
			PickupSeeds = GetBool(xmlDocument, "pickup_seeds", PickupSeeds);
			PickupOres = GetBool(xmlDocument, "pickup_ores", PickupOres);
			PickupResources = GetBool(xmlDocument, "pickup_resources", PickupResources);
			PickupBuilding = GetBool(xmlDocument, "pickup_building", PickupBuilding);
			PickupChemicals = GetBool(xmlDocument, "pickup_chemicals", PickupChemicals);
			PickupMisc = GetBool(xmlDocument, "pickup_misc", PickupMisc);
			AmmoRunnerEnabled = GetBool(xmlDocument, "ammo_runner_enabled", AmmoRunnerEnabled);
			AmmoRunnerThreshold = (int)GetFloat(xmlDocument, "ammo_runner_threshold", AmmoRunnerThreshold);
			AmmoRunnerAmount = (int)GetFloat(xmlDocument, "ammo_runner_amount", AmmoRunnerAmount);
			ResupplyCooldown = GetFloat(xmlDocument, "resupply_cooldown", ResupplyCooldown);
			MedicRunnerEnabled = GetBool(xmlDocument, "medic_runner_enabled", MedicRunnerEnabled);
			MedicRunnerThreshold = (int)GetFloat(xmlDocument, "medic_runner_threshold", MedicRunnerThreshold);
			MedicRunnerAmount = (int)GetFloat(xmlDocument, "medic_runner_amount", MedicRunnerAmount);
			string text2 = Get(xmlDocument, "medic_items");
			if (text2 != null)
			{
				List<string> list = new List<string>();
				string[] array = text2.Split(',');
				for (int i = 0; i < array.Length; i++)
				{
					string text3 = array[i].Trim();
					if (text3.Length > 0)
					{
						list.Add(text3);
					}
				}
				MedicItems = list.ToArray();
			}
			SortLootEnabled = GetBool(xmlDocument, "sort_loot_enabled", SortLootEnabled);
			SortIconColor = ReadColor(xmlDocument, "sort_icon_color", SortIconColor);
			VacuumOnColor = ReadColor(xmlDocument, "vacuum_on_color", VacuumOnColor);
			VacuumOffColor = ReadColor(xmlDocument, "vacuum_off_color", VacuumOffColor);
			DepositEnabled = GetBool(xmlDocument, "deposit_enabled", DepositEnabled);
			DepositWhenFull = GetBool(xmlDocument, "deposit_when_full", DepositWhenFull);
			DepositRadius = GetFloat(xmlDocument, "deposit_radius", DepositRadius);
			string text4 = Get(xmlDocument, "deposit_label");
			if (!string.IsNullOrEmpty(text4))
			{
				DepositLabel = text4.Trim();
			}
			RecallEnabled = GetBool(xmlDocument, "recall_enabled", RecallEnabled);
			string text5 = Get(xmlDocument, "recall_key");
			if (!string.IsNullOrEmpty(text5) && Enum.TryParse<KeyCode>(text5.Trim(), ignoreCase: true, out KeyCode result))
			{
				RecallKey = result;
			}
			if (Radius < 0f)
			{
				Radius = 0f;
			}
			if (DepositRadius < 0f)
			{
				DepositRadius = 0f;
			}
			if (ScanInterval < 0.05f)
			{
				ScanInterval = 0.05f;
			}
			Log(string.Format("enabled={0} radius={1} scan_interval={2} zombie_bags_only={3} vacuum_world_containers={4} only_loot_untouched={5} remove_empty_containers={6} remove_names=[{7}] downgrade_names=[{8}] ", Enabled, Radius, ScanInterval, ZombieBagsOnly, VacuumWorldContainers, OnlyLootUntouchedContainers, RemoveEmptyContainers, string.Join(",", RemoveContainerNames), string.Join(",", DowngradeContainerNames)) + $"filter[ammo={PickupAmmo} weapons={PickupWeapons} tools={PickupTools} armor={PickupArmor} clothing={PickupClothing} medical={PickupMedical} food={PickupFood} drinks={PickupDrinks} books={PickupBooks}(read={PickupReadBooks}) mods={PickupMods} seeds={PickupSeeds} ores={PickupOres} resources={PickupResources} building={PickupBuilding} chemicals={PickupChemicals} misc={PickupMisc}] " + $"deposit_enabled={DepositEnabled} deposit_label='{DepositLabel}' deposit_radius={DepositRadius} deposit_when_full={DepositWhenFull}");
		}
		catch (Exception ex)
		{
			Log("config load failed: " + ex.Message);
		}
	}

	private static Color ReadColor(XmlDocument doc, string name, Color def)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		string text = Get(doc, name);
		if (string.IsNullOrEmpty(text))
		{
			return def;
		}
		if (!text.StartsWith("#"))
		{
			text = "#" + text;
		}
		Color result = default(Color);
		if (ColorUtility.TryParseHtmlString(text, ref result))
		{
			return result;
		}
		Log("bad " + name + " '" + text + "' - want an HTML hex like #64DC78; using default");
		return def;
	}

	private static string Get(XmlDocument doc, string name)
	{
		return doc.SelectSingleNode("//set[@name='" + name + "']")?.Attributes?["value"]?.Value;
	}

	private static string[] GetNames(XmlDocument doc, string name, string[] def)
	{
		string text = Get(doc, name);
		if (text == null)
		{
			return def;
		}
		List<string> list = new List<string>();
		string[] array = text.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim().ToLowerInvariant();
			if (text2.Length > 0)
			{
				list.Add(text2);
			}
		}
		return list.ToArray();
	}

	private static float GetFloat(XmlDocument doc, string name, float def)
	{
		string text = Get(doc, name);
		if (string.IsNullOrEmpty(text) || !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return def;
		}
		return result;
	}

	private static bool GetBool(XmlDocument doc, string name, bool def)
	{
		string value = Get(doc, name);
		if (string.IsNullOrEmpty(value) || !bool.TryParse(value, out var result))
		{
			return def;
		}
		return result;
	}

	public static void Log(string m)
	{
		Log.Out("[DroneLootVacuum] " + m);
	}
}
