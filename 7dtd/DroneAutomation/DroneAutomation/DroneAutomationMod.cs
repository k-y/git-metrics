using System;
using System.IO;
using System.Reflection;
using System.Xml;
using HarmonyLib;

namespace DroneAutomation;

public class DroneAutomationMod : IModApi
{
	public const string HarmonyId = "com.tehaon.droneautomation";

	public const string AutoLootModuleName = "modRoboticDroneAutoLootMod";

	public const string AutoSalvageModuleName = "modRoboticDroneAutoSalvageMod";

	public const string AutoHarvestModuleName = "modRoboticDroneAutoHarvestMod";

	public const string AutoRepairModuleName = "modRoboticDroneAutoRepairMod";

	public const string AutoPlantModuleName = "modRoboticDroneAutoPlantMod";

	public const string AutoDefenseModuleName = "modRoboticDroneAutoDefenseMod";

	public const string OverclockModuleName = "modRoboticDroneOverclockMod";

	public const string AntennaModuleName = "modRoboticDroneAntennaMod";

	public const string LootOffCVar = "daLootOff";

	public const string SalvageOffCVar = "daSalvageOff";

	public const string HarvestOffCVar = "daHarvestOff";

	public const string RepairOffCVar = "daRepairOff";

	public const string PlantOffCVar = "daPlantOff";

	public const string DefenseOffCVar = "daDefenseOff";

	public const string LootMissingCVar = "daLootMissing";

	public const string SalvageMissingCVar = "daSalvageMissing";

	public const string HarvestMissingCVar = "daHarvestMissing";

	public const string RepairMissingCVar = "daRepairMissing";

	public const string PlantMissingCVar = "daPlantMissing";

	public const string DefenseMissingCVar = "daDefenseMissing";

	public static readonly string[] ToggleBuffs = new string[13]
	{
		"buffDaLootDisable", "buffDaLootEnable", "buffDaSalvageDisable", "buffDaSalvageEnable", "buffDaHarvestDisable", "buffDaHarvestEnable", "buffDaRepairDisable", "buffDaRepairEnable", "buffDaPlantDisable", "buffDaPlantEnable",
		"buffDaDefenseDisable", "buffDaDefenseEnable", "buffDaAllEnable"
	};

	public static string ModPath;

	public static bool Debug;

	public static bool WorkWhileParked = true;

	public static float MaxOwnerDistance = 25f;

	public static VacuumSettings AutoLootSettings = new VacuumSettings
	{
		ContainerRadius = 8f,
		EntityRadius = 15f,
		VerticalRadius = 6f
	};

	public static SalvageSettings SalvageSettings = new SalvageSettings();

	public static HarvestSettings HarvestSettings = new HarvestSettings();

	public static RepairSettings RepairSettings = new RepairSettings();

	public static PlantSettings PlantSettings = new PlantSettings();

	public static DefenseSettings DefenseSettings = new DefenseSettings();

	public static OverclockSettings OverclockSettings = new OverclockSettings();

	public static AntennaSettings AntennaSettings = new AntennaSettings();

	public void InitMod(Mod _modInstance)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ModPath = _modInstance.Path;
			LoadSettings();
			Harmony val = new Harmony("com.tehaon.droneautomation");
			val.PatchAll(Assembly.GetExecutingAssembly());
			int num = 0;
			foreach (MethodBase patchedMethod in val.GetPatchedMethods())
			{
				_ = patchedMethod;
				num++;
			}
			if (num == 0)
			{
				Log.Warning("[DroneAutomation] No methods patched — every module will do nothing.");
			}
			Log.Out($"[DroneAutomation] InitMod complete — {num} method(s) patched, server-side only. Path: {ModPath}");
		}
		catch (Exception ex)
		{
			Log.Error("[DroneAutomation] InitMod failed: " + ex);
		}
	}

	private static void ApplyLocalOverrides(XmlDocument _doc)
	{
		string text = Path.Combine(ModPath, "droneautomation.local.xml");
		if (!File.Exists(text))
		{
			return;
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(text);
			XmlElement documentElement = _doc.DocumentElement;
			XmlElement documentElement2 = xmlDocument.DocumentElement;
			if (documentElement != null && documentElement2 != null)
			{
				int num = MergeInto(documentElement, documentElement2);
				Log.Out($"[DroneAutomation] droneautomation.local.xml applied ({num} override(s)).");
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[DroneAutomation] Ignoring malformed droneautomation.local.xml: " + ex.Message);
		}
	}

	private static int MergeInto(XmlElement _base, XmlElement _local)
	{
		int num = 0;
		foreach (XmlAttribute attribute in _local.Attributes)
		{
			_base.SetAttribute(attribute.Name, attribute.Value);
			num++;
		}
		foreach (XmlNode childNode in _local.ChildNodes)
		{
			if (childNode is XmlElement xmlElement)
			{
				XmlElement xmlElement2 = FindChild(_base, xmlElement);
				if (xmlElement2 == null)
				{
					_base.AppendChild(_base.OwnerDocument.ImportNode(xmlElement, deep: true));
					num++;
				}
				else
				{
					num += MergeInto(xmlElement2, xmlElement);
				}
			}
		}
		return num;
	}

	private static XmlElement FindChild(XmlElement _parent, XmlElement _like)
	{
		string text = _like.GetAttribute("name");
		if (string.IsNullOrEmpty(text))
		{
			text = null;
		}
		foreach (XmlNode childNode in _parent.ChildNodes)
		{
			if (childNode is XmlElement xmlElement && !(xmlElement.Name != _like.Name))
			{
				if (text == null)
				{
					return xmlElement;
				}
				if (xmlElement.GetAttribute("name") == text)
				{
					return xmlElement;
				}
			}
		}
		return null;
	}

	private static void LoadSettings()
	{
		string text = Path.Combine(ModPath, "droneautomation.xml");
		if (!File.Exists(text))
		{
			AutoLootSettings.Clamp();
			return;
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(text);
			ApplyLocalOverrides(xmlDocument);
			ReadVacuum(xmlDocument.SelectSingleNode("/droneautomation/autoLoot"), AutoLootSettings);
			ReadSalvage(xmlDocument.SelectSingleNode("/droneautomation/autoSalvage"), SalvageSettings);
			ReadHarvest(xmlDocument.SelectSingleNode("/droneautomation/autoHarvest"), HarvestSettings);
			ReadRepair(xmlDocument.SelectSingleNode("/droneautomation/autoRepair"), RepairSettings);
			ReadPlant(xmlDocument.SelectSingleNode("/droneautomation/autoPlant"), PlantSettings);
			ReadDefense(xmlDocument.SelectSingleNode("/droneautomation/autoDefense"), DefenseSettings);
			ReadOverclock(xmlDocument.SelectSingleNode("/droneautomation/overclock"), OverclockSettings);
			ReadAntenna(xmlDocument.SelectSingleNode("/droneautomation/antenna"), AntennaSettings);
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/droneautomation");
			if (xmlNode?.Attributes != null)
			{
				ReadBool(xmlNode, "Debug", ref Debug);
				ReadBool(xmlNode, "WorkWhileParked", ref WorkWhileParked);
				ReadFloat(xmlNode, "MaxOwnerDistance", ref MaxOwnerDistance);
				if (MaxOwnerDistance < 0f)
				{
					MaxOwnerDistance = 0f;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[DroneAutomation] Could not read droneautomation.xml, using defaults: " + ex.Message);
		}
		AutoLootSettings.Clamp();
		SalvageSettings.Clamp();
		HarvestSettings.Clamp();
		RepairSettings.Clamp();
		PlantSettings.Clamp();
		DefenseSettings.Clamp();
		OverclockSettings.Clamp();
		AntennaSettings.Clamp();
	}

	private static void ReadRepair(XmlNode _node, RepairSettings _settings)
	{
		if (_node?.Attributes != null)
		{
			ReadFloat(_node, "Radius", ref _settings.Radius);
			ReadFloat(_node, "VerticalRadius", ref _settings.VerticalRadius);
			ReadFloat(_node, "SecondsPerBlock", ref _settings.SecondsPerBlock);
			ReadFloat(_node, "MaxCatchupSeconds", ref _settings.MaxCatchupSeconds);
			ReadFloat(_node, "LowQualityReach", ref _settings.LowQualityReach);
			ReadFloat(_node, "LowQualityTimeMult", ref _settings.LowQualityTimeMult);
		}
	}

	private static void ReadSalvage(XmlNode _node, SalvageSettings _settings)
	{
		if (_node?.Attributes == null)
		{
			return;
		}
		ReadFloat(_node, "Radius", ref _settings.Radius);
		ReadFloat(_node, "VerticalRadius", ref _settings.VerticalRadius);
		ReadFloat(_node, "SecondsPerStep", ref _settings.SecondsPerStep);
		ReadFloat(_node, "MaxCatchupSeconds", ref _settings.MaxCatchupSeconds);
		ReadFloat(_node, "LowQualityReach", ref _settings.LowQualityReach);
		ReadFloat(_node, "LowQualityTimeMult", ref _settings.LowQualityTimeMult);
		ReadBool(_node, "SalvageWorkstations", ref _settings.SalvageWorkstations);
		ReadBool(_node, "SalvageInPOIs", ref _settings.SalvageInPOIs);
		ReadBool(_node, "SalvageSwitches", ref _settings.SalvageSwitches);
		_settings.ExcludedBlocks.Clear();
		foreach (XmlNode childNode in _node.ChildNodes)
		{
			if (!(childNode.Name != "exclude"))
			{
				string text = childNode.Attributes?["block"]?.Value;
				if (!string.IsNullOrEmpty(text))
				{
					_settings.ExcludedBlocks.Add(text);
				}
			}
		}
	}

	private static void ReadHarvest(XmlNode _node, HarvestSettings _settings)
	{
		if (_node?.Attributes != null)
		{
			ReadFloat(_node, "Radius", ref _settings.Radius);
			ReadFloat(_node, "VerticalRadius", ref _settings.VerticalRadius);
			ReadFloat(_node, "SecondsPerTarget", ref _settings.SecondsPerTarget);
			ReadFloat(_node, "MaxCatchupSeconds", ref _settings.MaxCatchupSeconds);
			ReadFloat(_node, "LowQualityReach", ref _settings.LowQualityReach);
			ReadFloat(_node, "LowQualityTimeMult", ref _settings.LowQualityTimeMult);
		}
	}

	private static void ReadVacuum(XmlNode _node, VacuumSettings _settings)
	{
		if (_node?.Attributes != null)
		{
			ReadFloat(_node, "Radius", ref _settings.ContainerRadius);
			ReadFloat(_node, "EntityRadius", ref _settings.EntityRadius);
			ReadFloat(_node, "VerticalRadius", ref _settings.VerticalRadius);
			ReadFloat(_node, "SpeedMultiplier", ref _settings.SpeedMultiplier);
			ReadFloat(_node, "MinSecondsPerTarget", ref _settings.MinSecondsPerTarget);
			ReadFloat(_node, "ItemPickupSeconds", ref _settings.ItemPickupSeconds);
			ReadFloat(_node, "MaxCatchupSeconds", ref _settings.MaxCatchupSeconds);
			ReadFloat(_node, "SkipIfPlayerWithin", ref _settings.SkipIfPlayerWithin);
			ReadFloat(_node, "ThrownGraceSeconds", ref _settings.ThrownGraceSeconds);
			ReadFloat(_node, "LowQualityReach", ref _settings.LowQualityReach);
			ReadFloat(_node, "LowQualityTimeMult", ref _settings.LowQualityTimeMult);
		}
	}

	private static void ReadPlant(XmlNode _node, PlantSettings _settings)
	{
		if (_node?.Attributes != null)
		{
			ReadFloat(_node, "Radius", ref _settings.Radius);
			ReadFloat(_node, "VerticalRadius", ref _settings.VerticalRadius);
			ReadFloat(_node, "SecondsPerPlant", ref _settings.SecondsPerPlant);
			ReadFloat(_node, "MaxCatchupSeconds", ref _settings.MaxCatchupSeconds);
			ReadFloat(_node, "LowQualityReach", ref _settings.LowQualityReach);
			ReadFloat(_node, "LowQualityTimeMult", ref _settings.LowQualityTimeMult);
		}
	}

	private static void ReadDefense(XmlNode _node, DefenseSettings _settings)
	{
		if (_node?.Attributes != null)
		{
			ReadFloat(_node, "Range", ref _settings.Range);
			ReadFloat(_node, "SecondsPerShot", ref _settings.SecondsPerShot);
			ReadFloat(_node, "MaxCatchupSeconds", ref _settings.MaxCatchupSeconds);
			ReadFloat(_node, "LowQualityReach", ref _settings.LowQualityReach);
			ReadFloat(_node, "LowQualityTimeMult", ref _settings.LowQualityTimeMult);
			ReadBool(_node, "RequireLineOfSight", ref _settings.RequireLineOfSight);
		}
	}

	private static void ReadOverclock(XmlNode _node, OverclockSettings _settings)
	{
		if (_node?.Attributes != null)
		{
			ReadFloat(_node, "Q1TimeMult", ref _settings.Q1TimeMult);
			ReadFloat(_node, "Q6TimeMult", ref _settings.Q6TimeMult);
		}
	}

	private static void ReadAntenna(XmlNode _node, AntennaSettings _settings)
	{
		if (_node?.Attributes != null)
		{
			ReadFloat(_node, "Q1ReachMult", ref _settings.Q1ReachMult);
			ReadFloat(_node, "Q6ReachMult", ref _settings.Q6ReachMult);
		}
	}

	internal static void ReadFloat(XmlNode _node, string _attr, ref float _value)
	{
		string text = _node.Attributes[_attr]?.Value;
		float num = default(float);
		if (!string.IsNullOrEmpty(text) && StringParsers.TryParseFloat(text, ref num))
		{
			_value = num;
		}
	}

	internal static void ReadBool(XmlNode _node, string _attr, ref bool _value)
	{
		string text = _node.Attributes[_attr]?.Value;
		if (!string.IsNullOrEmpty(text))
		{
			_value = text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
		}
	}
}
