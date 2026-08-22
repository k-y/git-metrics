using System.IO;

namespace DroneLootVacuum;

public class NetPackageDroneStorage : NetPackage
{
	private int _droneId;

	private int _playerId;

	private int _busy;

	public NetPackageDroneStorage Setup(int _drone, int _player, bool _isBusy)
	{
		_droneId = _drone;
		_playerId = _player;
		_busy = (_isBusy ? 1 : 0);
		return this;
	}

	public override void write(PooledBinaryWriter _bw)
	{
		((NetPackage)this).write(_bw);
		Stream baseStream = ((BinaryWriter)(object)_bw).BaseStream;
		WriteInt(baseStream, _droneId);
		WriteInt(baseStream, _playerId);
		WriteInt(baseStream, _busy);
	}

	public override void read(PooledBinaryReader _br)
	{
		Stream baseStream = ((BinaryReader)(object)_br).BaseStream;
		_droneId = ReadInt(baseStream);
		_playerId = ReadInt(baseStream);
		_busy = ReadInt(baseStream);
	}

	public override void ProcessPackage(World _world, GameManager _callbacks)
	{
		if (_world != null && (((NetPackage)this).Sender == null || ((NetPackage)this).ValidEntityIdForSender(_playerId, false)))
		{
			DroneBusy.Set(_droneId, _busy != 0);
		}
	}

	public override int GetLength()
	{
		return 20;
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
