using System;
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

        private static byte[] _hexLookup => new byte[]
        {
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0
        };

        private static char[] _charLookup => new char[]
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
        };

        public PayloadParser(string payloadString)
        {
            _bytes = ConvertFromHexString(payloadString);
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
            ushort result = BitConverter.ToUInt16(_bytes, _bytePos);
            _bytePos += 2;
            return result;
        }

        public short GetInt16()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(16);
            short result = BitConverter.ToInt16(_bytes, _bytePos);
            _bytePos += 2;
            return result;
        }

        public ushort GetUInt16BE()
        {
            byte[] bytes = new byte[2];
            bytes[1] = GetUInt8();
            bytes[0] = GetUInt8();
            return BitConverter.ToUInt16(bytes, 0);
        }

        public short GetInt16BE()
        {
            byte[] bytes = new byte[2];
            bytes[1] = GetUInt8();
            bytes[0] = GetUInt8();
            return BitConverter.ToInt16(bytes, 0);
        }

        public int GetUInt24()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(24);
            int result =
                _bytes[_bytePos + 0] +
                _bytes[_bytePos + 1] * 256 +
                _bytes[_bytePos + 2] * 65536
            ;
            _bytePos += 3;
            return result;
        }

        public int GetInt24()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(24);
            int result = (
                _bytes[_bytePos + 0] +
                _bytes[_bytePos + 1] * 256 +
                _bytes[_bytePos + 2] * 65536
            );
            if ((_bytes[_bytePos + 2] & 0x80) > 0)
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
            uint result = BitConverter.ToUInt32(_bytes, _bytePos);
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
            int result = BitConverter.ToInt32(_bytes, _bytePos);
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
            return BitConverter.ToUInt32(bytes, 0);
        }

        public int GetInt32BE()
        {
            byte[] bytes = new byte[4];
            bytes[3] = GetUInt8();
            bytes[2] = GetUInt8();
            bytes[1] = GetUInt8();
            bytes[0] = GetUInt8();
            return BitConverter.ToInt32(bytes, 0);
        }

        public ulong GetUInt64()
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(64);
            ulong result = BitConverter.ToUInt64(_bytes, _bytePos);
            _bytePos += 8;
            return result;
        }

        public string GetHexString(int byteCount)
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(byteCount * 8);
            string result = ConvertToHexString(_bytes, _bytePos, byteCount);
            _bytePos += byteCount;
            return result;
        }

        public string GetString(int byteCount)
        {
            MoveToNextWholeByte();
            ThrowIfNotEnoughBits(byteCount * 8);
            string result = Encoding.UTF8.GetString(_bytes, _bytePos, byteCount);
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

        private static byte[] ConvertFromHexString(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                throw new ArgumentException("Hex string cannot be null or an empty string", nameof(hex));
            }
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex string must contain whole bytes", nameof(hex));
            }
            byte[] bytes = new byte[hex.Length / 2];
            int i = 0;
            int j = 0;
            while (j < bytes.Length)
            {
                byte lo = _hexLookup[(byte)hex[i + 1]];
                byte hi = _hexLookup[(byte)hex[i]];
                bytes[j++] = (byte)(hi << 4 | lo);
                i += 2;
            }
            return bytes;
        }

        private static string ConvertToHexString(byte[] bytes, int startIndex, int byteCount)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (startIndex < 0 || startIndex >= bytes.Length)
            {
                throw new ArgumentException("Start index out of bounds of the array", nameof(startIndex));
            }

            if (byteCount < 0)
            {
                throw new ArgumentException("Byte count cannot be less than zero", nameof(byteCount));
            }

            if (bytes.Length < startIndex + byteCount)
            {
                throw new ArgumentException("Not enough bytes", nameof(bytes));
            }

            int i = startIndex;
            int end = startIndex + byteCount;
            StringBuilder sb = new StringBuilder(byteCount * 2);
            while (i < end)
            {
                sb.Append(_charLookup[bytes[i] >> 4]);
                sb.Append(_charLookup[bytes[i] & 0xf]);
                i++;
            }
            return sb.ToString();
        }
    }
}
