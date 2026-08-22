using System.IO;
using UnityEngine;

namespace DroneLootVacuum;

public class NetPackageDroneGiveItem : NetPackage
{
	private int _playerId;

	private int _itemType;

	private int _count;

	public NetPackageDroneGiveItem Setup(int _player, int _type, int _amount)
	{
		_playerId = _player;
		_itemType = _type;
		_count = _amount;
		return this;
	}

	public override void write(PooledBinaryWriter _bw)
	{
		((NetPackage)this).write(_bw);
		Stream baseStream = ((BinaryWriter)(object)_bw).BaseStream;
		WriteInt(baseStream, _playerId);
		WriteInt(baseStream, _itemType);
		WriteInt(baseStream, _count);
	}

	public override void read(PooledBinaryReader _br)
	{
		Stream baseStream = ((BinaryReader)(object)_br).BaseStream;
		_playerId = ReadInt(baseStream);
		_itemType = ReadInt(baseStream);
		_count = ReadInt(baseStream);
	}

	public override void ProcessPackage(World _world, GameManager _callbacks)
	{
		if (_world != null && _count > 0)
		{
			EntityPlayerLocal primaryPlayer = ((WorldBase)_world).GetPrimaryPlayer();
			if (!((Object)(object)primaryPlayer == (Object)null) && ((Entity)primaryPlayer).entityId == _playerId)
			{
				Resupply.GiveLocal((EntityPlayer)(object)primaryPlayer, _itemType, _count);
			}
		}
	}

	public override int GetLength()
	{
		return 16;
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
