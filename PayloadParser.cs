using System.Text;

namespace IoTPayloadDecoder
{
    internal class PayloadParser
    {
        public int RemainingBits
        {
            get
            {
                return _bitCount - _bytePos * 8 - _bitOffset;
            }
        }

        private readonly byte[] _bytes;
        private readonly int _bitCount;
        private int _bytePos;
        private byte _bitOffset;

        public PayloadParser(string payloadString)
        {
            _bytes = Convert.FromHexString(payloadString);
            _bitCount = _bytes.Length * 8;
        }

        public PayloadParser(byte[] bytes)
        {
            _bytes = new byte[bytes.Length];
            Array.Copy(bytes, _bytes, _bytes.Length);
            _bitCount = _bytes.Length * 8;
        }

        public PayloadParser(byte oneByte) : this(new byte[] { oneByte }) {}

        public byte GetUInt8(bool peek = false)
        {
            int bytePos = _bytePos;
            byte offset = _bitOffset;
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(8);
            byte result = _bytes[_bytePos];
            if (peek)
            {
                _bytePos = bytePos;
                _bitOffset = offset;
                return result;
            }
            _bytePos++;
            return result;
        }

        public sbyte GetInt8()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(8);
            return (sbyte)_bytes[_bytePos++];
        }

        public ushort GetUInt16()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(16);
            ushort result = BitConverter.ToUInt16(new ReadOnlySpan<byte>(_bytes, _bytePos, 2));
            _bytePos += 2;
            return result;
        }

        public short GetInt16()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(16);
            short result = BitConverter.ToInt16(new ReadOnlySpan<byte>(_bytes, _bytePos, 2));
            _bytePos += 2;
            return result;
        }

        public ushort GetUInt16BE()
        {
            byte[] bytes = new byte[2];
            bytes[1] = GetUInt8();
            bytes[0] = GetUInt8();
            return BitConverter.ToUInt16(new ReadOnlySpan<byte>(bytes));
        }

        public short GetInt16BE()
        {
            byte[] bytes = new byte[2];
            bytes[1] = GetUInt8();
            bytes[0] = GetUInt8();
            return BitConverter.ToInt16(new ReadOnlySpan<byte>(bytes));
        }

        public int GetUInt24()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(24);
            ReadOnlySpan<byte> span = new(_bytes, _bytePos, 3);
            int result =
                span[0] +
                span[1] * 256 +
                span[2] * 65536
            ;
            _bytePos += 3;
            return result;
        }

        public int GetInt24()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(24);
            ReadOnlySpan<byte> span = new(_bytes, _bytePos, 3);         
            int result = (
                span[0] +
                span[1] * 256 +
                span[2] * 65536
            );
            if ((span[2] & 0x80) > 0)
            {
                result |= (0xFF << 24);
            }
            _bytePos += 3;
            return result;
        }

        public uint GetUInt32(bool peek = false)
        {
            int bytePos = _bytePos;
            byte offset = _bitOffset;
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(32);
            uint result = BitConverter.ToUInt32(new ReadOnlySpan<byte>(_bytes, _bytePos, 4));
            if (peek)
            {
                _bytePos = bytePos;
                _bitOffset = offset;
                return result;
            }
            _bytePos += 4;
            return result;
        }

        public int GetInt32()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(32);
            int result = BitConverter.ToInt32(new ReadOnlySpan<byte>(_bytes, _bytePos, 4));
            _bytePos += 4;
            return result;
        }

        public uint GetUInt32BE()
        {
            byte[] bytes = new byte[4];
            bytes[3] = GetUInt8();
            bytes[2] = GetUInt8();
            bytes[1] = GetUInt8();
            bytes[0] = GetUInt8();
            return BitConverter.ToUInt32(new ReadOnlySpan<byte>(bytes));
        }

        public int GetInt32BE()
        {
            byte[] bytes = new byte[4];
            bytes[3] = GetUInt8();
            bytes[2] = GetUInt8();
            bytes[1] = GetUInt8();
            bytes[0] = GetUInt8();
            return BitConverter.ToInt32(new ReadOnlySpan<byte>(bytes));
        }

        public ulong GetUInt64()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(64);
            ulong result = BitConverter.ToUInt64(new ReadOnlySpan<byte>(_bytes, _bytePos, 8));
            _bytePos += 8;
            return result;
        }

        public string GetHexString(int byteCount)
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(byteCount * 8);
            string result = Convert.ToHexString(new ReadOnlySpan<byte>(_bytes, _bytePos, byteCount));
            _bytePos += byteCount;
            return result;
        }

        public string GetString(int byteCount)
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(byteCount * 8);
            string result = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(_bytes, _bytePos, byteCount));
            _bytePos += byteCount;
            return result;
        }

        public bool GetBit()
        {
            if (_bitOffset >= 8)
            {
                MoveToNextWholeByte();
            }
            ThrowIfNotEnoughBits(1);
            return ((_bytes[_bytePos] >> _bitOffset++) & 1) == 1;
        }

        public byte GetBits(int bitCount)
        {
            if (bitCount < 2)
            {
                throw new InvalidOperationException("Cannot extract less than two bits. To extract one bit, use GetBit().");
            }
            if (bitCount > 7)
            {
                throw new InvalidOperationException("Cannot extract more than 7 bits at a time.");
            }
            if (_bitOffset >= 8)
            {
                MoveToNextWholeByte();
            }
            ThrowIfNotEnoughBits(bitCount);
            byte mask = (byte)(Math.Pow(2, bitCount) - 1);
            byte result = (byte)((_bytes[_bytePos] >> _bitOffset) & mask);
            _bitOffset += (byte)bitCount;
            return result;
        }

        public DateTime GetUnixEpoch()
        {
            uint epoch = GetUInt32();
            return DateTimeOffset.FromUnixTimeSeconds(epoch).DateTime;
        }

        private void MoveToNextWholeByte()
        {
            if (_bitOffset == 0)
            {
                return;
            }

            _bitOffset = 0;
            _bytePos++;
        }

        private void ThrowIfNotEnoughBits(int bitsRequested)
        {
            if (_bytePos * 8 + _bitOffset + bitsRequested > _bitCount)
            {
                throw new InvalidOperationException("Not enough bits left to fullfill request.");
            }
        }
    }
}
