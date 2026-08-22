using System.Collections.Generic;
using UnityEngine;

namespace DroneLootVacuum;

public class ConsoleCmdDroneVac : ConsoleCmdAbstract
{
	public override string[] getCommands()
	{
		return new string[2] { "dronevac", "dlv" };
	}

	public override string getDescription()
	{
		return "DroneLootVacuum: reloadcfg | diag | recall | status.";
	}

	public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		switch ((_params.Count > 0) ? _params[0].ToLowerInvariant() : "status")
		{
		case "reloadcfg":
			Config.Load(ModPathHolder.Path);
			Log.Out(Status());
			break;
		case "recall":
		{
			GameManager instance = GameManager.Instance;
			World val = ((instance != null) ? instance.World : null);
			EntityPlayer val2 = (EntityPlayer)((_senderInfo.RemoteClientInfo != null) ? ((object)/*isinst with value type is only supported in some contexts*/) : ((object)((val != null) ? ((WorldBase)val).GetPrimaryPlayer() : null)));
			if ((Object)(object)val2 == (Object)null)
			{
				Log.Out("[DroneLootVacuum] recall: no player to recall for");
			}
			else
			{
				DroneRecall.Request(val2);
			}
			break;
		}
		case "diag":
			DroneOnUpdatePatch.Diag = !DroneOnUpdatePatch.Diag;
			Log.Out($"[DroneLootVacuum] diag override = {DroneOnUpdatePatch.Diag} (config debug={Config.DebugLog}); watch for [DLV-*] lines near containers");
			break;
		default:
			Log.Out(Status());
			break;
		}
	}

	private static string Status()
	{
		return $"[DroneLootVacuum] enabled={Config.Enabled} zombie_bags_only={Config.ZombieBagsOnly} " + $"vacuum_world_containers={Config.VacuumWorldContainers} pickup_radius={Config.Radius} " + $"deposit_enabled={Config.DepositEnabled} debug={Config.DebugLog} diag_override={DroneOnUpdatePatch.Diag}";
	}
}
