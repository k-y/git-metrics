using System.IO;
using UnityEngine;

namespace DroneLootVacuum;

public class NetPackageDroneResupply : NetPackage
{
	private int _droneId;

	private int _playerId;

	private int _weaponType;

	private int _ammoIdx;

	private int _needMedical;

	public NetPackageDroneResupply Setup(int _drone, int _player, int _weapon, int _ammoSlot, bool _medical)
	{
		_droneId = _drone;
		_playerId = _player;
		_weaponType = _weapon;
		_ammoIdx = _ammoSlot;
		_needMedical = (_medical ? 1 : 0);
		return this;
	}

	public override void write(PooledBinaryWriter _bw)
	{
		((NetPackage)this).write(_bw);
		Stream baseStream = ((BinaryWriter)(object)_bw).BaseStream;
		WriteInt(baseStream, _droneId);
		WriteInt(baseStream, _playerId);
		WriteInt(baseStream, _weaponType);
		WriteInt(baseStream, _ammoIdx);
		WriteInt(baseStream, _needMedical);
	}

	public override void read(PooledBinaryReader _br)
	{
		Stream baseStream = ((BinaryReader)(object)_br).BaseStream;
		_droneId = ReadInt(baseStream);
		_playerId = ReadInt(baseStream);
		_weaponType = ReadInt(baseStream);
		_ammoIdx = ReadInt(baseStream);
		_needMedical = ReadInt(baseStream);
	}

	public override void ProcessPackage(World _world, GameManager _callbacks)
	{
		if (_world != null && (((NetPackage)this).Sender == null || ((NetPackage)this).ValidEntityIdForSender(_playerId, false)))
		{
			Entity entity = ((WorldBase)_world).GetEntity(_droneId);
			EntityDrone val = (EntityDrone)(object)((entity is EntityDrone) ? entity : null);
			Entity entity2 = ((WorldBase)_world).GetEntity(_playerId);
			EntityPlayer val2 = (EntityPlayer)(object)((entity2 is EntityPlayer) ? entity2 : null);
			if (!((Object)(object)val == (Object)null) && !((Object)(object)val2 == (Object)null))
			{
				Resupply.ServeRequest(val, val2, _weaponType, _ammoIdx, _needMedical != 0);
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
