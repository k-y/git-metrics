using System.IO;
using UnityEngine;

namespace DroneLootVacuum;

public class NetPackageDroneSortLoot : NetPackage
{
	public const int ActionSort = 0;

	public const int ActionToggle = 1;

	private int _droneId;

	private int _playerId;

	private int _action;

	private int _arg;

	private int _which;

	public NetPackageDroneSortLoot Setup(int _drone, int _player, int _act = 0, int _value = 0, int _switch = 0)
	{
		_droneId = _drone;
		_playerId = _player;
		_action = _act;
		_arg = _value;
		_which = _switch;
		return this;
	}

	public override void write(PooledBinaryWriter _bw)
	{
		((NetPackage)this).write(_bw);
		Stream baseStream = ((BinaryWriter)(object)_bw).BaseStream;
		WriteInt(baseStream, _droneId);
		WriteInt(baseStream, _playerId);
		WriteInt(baseStream, _action);
		WriteInt(baseStream, _arg);
		WriteInt(baseStream, _which);
	}

	public override void read(PooledBinaryReader _br)
	{
		Stream baseStream = ((BinaryReader)(object)_br).BaseStream;
		_droneId = ReadInt(baseStream);
		_playerId = ReadInt(baseStream);
		_action = ReadInt(baseStream);
		_arg = ReadInt(baseStream);
		_which = ReadInt(baseStream);
	}

	public override void ProcessPackage(World _world, GameManager _callbacks)
	{
		if (_world == null || (((NetPackage)this).Sender != null && !((NetPackage)this).ValidEntityIdForSender(_playerId, false)))
		{
			return;
		}
		Entity entity = ((WorldBase)_world).GetEntity(_droneId);
		EntityDrone val = (EntityDrone)(object)((entity is EntityDrone) ? entity : null);
		Entity entity2 = ((WorldBase)_world).GetEntity(_playerId);
		EntityPlayer val2 = (EntityPlayer)(object)((entity2 is EntityPlayer) ? entity2 : null);
		if (!((Object)(object)val == (Object)null) && !((Object)(object)val2 == (Object)null))
		{
			if (_action == 1)
			{
				DroneToggles.ByIndex(_which)?.Set(_droneId, _arg != 0);
			}
			else
			{
				LootSorter.SortFor(val, val2);
			}
		}
	}

	public override int GetLength()
	{
		return 28;
	}

	private static void WriteInt(Stream s, int v)
	{
		s.WriteByte((byte)v);
		s.WriteByte((byte)(v >> 8));
		s.WriteByte((byte)(v >> 16));
		s.WriteByte((byte)(v >> 24));
	}

	private static int ReadInt(Stream s)
	{
		int num = s.ReadByte();
		int num2 = s.ReadByte();
		int num3 = s.ReadByte();
		int num4 = s.ReadByte();
		return num | (num2 << 8) | (num3 << 16) | (num4 << 24);
	}
}
