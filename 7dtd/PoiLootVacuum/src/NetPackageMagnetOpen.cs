using System.IO;

namespace PoiLootVacuum
{
    public class NetPackageMagnetOpen : NetPackage
    {
        private int _playerId;
        private int _x, _y, _z;

        public NetPackageMagnetOpen Setup(int playerId, Vector3i pos)
        {
            _playerId = playerId;
            _x = pos.x; _y = pos.y; _z = pos.z;
            return this;
        }

        public override void write(PooledBinaryWriter _bw)
        {
            base.write(_bw);
            var s = ((BinaryWriter)(object)_bw).BaseStream;
            WriteInt(s, _playerId);
            WriteInt(s, _x);
            WriteInt(s, _y);
            WriteInt(s, _z);
        }

        public override void read(PooledBinaryReader _br)
        {
            var s = ((BinaryReader)(object)_br).BaseStream;
            _playerId = ReadInt(s);
            _x = ReadInt(s);
            _y = ReadInt(s);
            _z = ReadInt(s);
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            if (_world == null) return;
            if (Sender != null && !ValidEntityIdForSender(_playerId, false)) return;
            var player = ((WorldBase)_world).GetEntity(_playerId) as EntityPlayer;
            if (player == null) return;
            LootVacuumBehaviour.MagnetOpen(_world, player, new Vector3i(_x, _y, _z));
        }

        public override int GetLength() => 16;

        static void WriteInt(Stream s, int v)
        {
            s.WriteByte((byte)v);
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 24));
        }

        static int ReadInt(Stream s)
        {
            int b0 = s.ReadByte(), b1 = s.ReadByte(), b2 = s.ReadByte(), b3 = s.ReadByte();
            return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
        }
    }
}
