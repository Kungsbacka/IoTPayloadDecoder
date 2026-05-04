using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders.LHi110
{
    public class Port2PeriodicDataDecoder : IPayloadDecoder
    {
        public static int Port = 2;

        private List<string> _warnings;
        private PayloadParser _parser;
        private bool _compact;

        public dynamic Decode(string payloadString, bool compact)
        {
            if (string.IsNullOrWhiteSpace(payloadString))
            {
                throw new ArgumentException("Payload string cannot be empty", nameof(payloadString));
            }

            _warnings = new List<string>();
            _parser = new PayloadParser(payloadString);
            _compact = compact;

            dynamic result = DecodeUsagePacket();
            result.warnings = _warnings.ToArray();
            return result;
        }

        private dynamic DecodeUsagePacket()
        {
            dynamic data = new ExpandoObject();

            byte msgType = _parser.GetUInt8();
            uint timeStamp = _parser.GetUInt32BE();

            data.msgType = msgType;
            data.timeStamp = timeStamp;
            data.timeString = FormatUtcTime(timeStamp);

            switch (msgType)
            {
                case 0x01:
                    DecodeMsgType1(data);
                    break;

                default:
                    _warnings.Add($"Unsupported message type: 0x{msgType:X2}");
                    break;
            }

            dynamic result = new ExpandoObject();
            result.data = data;
            return result;
        }

        private void DecodeMsgType1(dynamic data)
        {
            data.activeImportReading = ReadUInt40BE();

            data.actL1ImportPowerPeak = DecodePower(_parser.GetUInt16BE());
            data.actL1ImportPowerAver = DecodePower(_parser.GetUInt16BE());
            data.actL2ImportPowerPeak = DecodePower(_parser.GetUInt16BE());
            data.actL2ImportPowerAver = DecodePower(_parser.GetUInt16BE());
            data.actL3ImportPowerPeak = DecodePower(_parser.GetUInt16BE());
            data.actL3ImportPowerAver = DecodePower(_parser.GetUInt16BE());

            uint voltagePacked = _parser.GetUInt32BE();
            int[] voltages = DecodeVoltages(voltagePacked);
            data.l1VoltageAver_mV = voltages[0];
            data.l2VoltageAver_mV = voltages[1];
            data.l3VoltageAver_mV = voltages[2];

            uint currentPacked = _parser.GetUInt32BE();
            long[] currents = DecodeCurrents(currentPacked);
            data.l1CurrentPeak_mA = currents[0];
            data.l2CurrentPeak_mA = currents[1];
            data.l3CurrentPeak_mA = currents[2];
        }

        // ===== Helpers =====
        private ulong ReadUInt40BE()
        {
            ulong b0 = _parser.GetUInt8();
            ulong b1 = _parser.GetUInt8();
            ulong b2 = _parser.GetUInt8();
            ulong b3 = _parser.GetUInt8();
            ulong b4 = _parser.GetUInt8();

            return (b0 << 32) |
                   (b1 << 24) |
                   (b2 << 16) |
                   (b3 << 8) |
                   b4;
        }

        private static int DecodePower(ushort rawValue)
        {
            bool useHundreds = (rawValue & (1 << 15)) != 0;

            if (useHundreds)
            {
                return (rawValue & ~(1 << 15)) * 100;
            }

            return rawValue;
        }

        private static int[] DecodeVoltages(uint packed)
        {
            int l1 = (int)((packed & 0x3FF00000) >> 20) * 250;
            int l2 = (int)((packed & 0x000FFC00) >> 10) * 250;
            int l3 = (int)(packed & 0x000003FF) * 250;

            if (l1 != 0) l1 += 20000;
            if (l2 != 0) l2 += 20000;
            if (l3 != 0) l3 += 20000;

            return new[] { l1, l2, l3 };
        }

        private static long[] DecodeCurrents(uint packed)
        {
            int scaleIndex = (int)((packed & 0xC0000000) >> 30);
            long scaleFactor = (long)Math.Pow(10, scaleIndex);

            long l1 = ((packed & 0x3FF00000) >> 20) * 100L * scaleFactor;
            long l2 = ((packed & 0x000FFC00) >> 10) * 100L * scaleFactor;
            long l3 = (packed & 0x000003FF) * 100L * scaleFactor;

            return new[] { l1, l2, l3 };
        }

        private static string FormatUtcTime(uint unixSeconds)
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}