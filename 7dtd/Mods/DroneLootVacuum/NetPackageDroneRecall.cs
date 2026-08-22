using System.IO;

namespace DroneLootVacuum;

public class NetPackageDroneRecall : NetPackage
{
	private int _playerId;

	public NetPackageDroneRecall Setup(int playerId)
	{
		_playerId = playerId;
		return this;
	}

	public override void write(PooledBinaryWriter _bw)
	{
		((NetPackage)this).write(_bw);
		WriteInt(((BinaryWriter)(object)_bw).BaseStream, _playerId);
	}

	public override void read(PooledBinaryReader _br)
	{
		_playerId = ReadInt(((BinaryReader)(object)_br).BaseStream);
	}

	public override void ProcessPackage(World _world, GameManager _callbacks)
	{
		if (_world != null && (((NetPackage)this).Sender == null || ((NetPackage)this).ValidEntityIdForSender(_playerId, false)))
		{
			Entity entity = ((WorldBase)_world).GetEntity(_playerId);
			EntityPlayer val = (EntityPlayer)(object)((entity is EntityPlayer) ? entity : null);
			if (val != null)
			{
				DroneRecall.RecallFor(val);
			}
		}
	}

	public override int GetLength()
	{
		return 12;
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
