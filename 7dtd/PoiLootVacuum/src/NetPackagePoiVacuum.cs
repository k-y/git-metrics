using System;
using System.IO;

namespace PoiLootVacuum
{
    public class NetPackagePoiVacuum : NetPackage
    {
        private int   _playerId;
        private float _radius;

        public NetPackagePoiVacuum Setup(int playerId, float radius)
        {
            _playerId = playerId;
            _radius   = radius;
            return this;
        }

        public override void write(PooledBinaryWriter _bw)
        {
            base.write(_bw);
            var s = ((BinaryWriter)(object)_bw).BaseStream;
            WriteInt(s, _playerId);
            WriteFloat(s, _radius);
        }

        public override void read(PooledBinaryReader _br)
        {
            var s = ((BinaryReader)(object)_br).BaseStream;
            _playerId = ReadInt(s);
            _radius   = ReadFloat(s);
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            if (_world == null) return;
            if (Sender != null && !ValidEntityIdForSender(_playerId, false)) return;
            var player = ((WorldBase)_world).GetEntity(_playerId) as EntityPlayer;
            if (player == null) return;
            LootVacuumBehaviour.Collect(_world, player, _radius, false);
        }

        public override int GetLength() => 8;

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

        static void WriteFloat(Stream s, float v)
        {
            var b = BitConverter.GetBytes(v);
            s.Write(b, 0, 4);
        }

        static float ReadFloat(Stream s)
        {
            var b = new byte[4];
            s.Read(b, 0, 4);
            return BitConverter.ToSingle(b, 0);
        }
    }
}
