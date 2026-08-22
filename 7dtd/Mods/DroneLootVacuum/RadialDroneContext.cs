using HarmonyLib;

namespace DroneLootVacuum;

[HarmonyPatch(typeof(XUiC_Radial), "SetCurrentEntityData")]
public static class RadialDroneContext
{
	public static EntityDrone Current;

	private static void Prefix(Entity _entity)
	{
		Current = (EntityDrone)(object)((_entity is EntityDrone) ? _entity : null);
	}
}
