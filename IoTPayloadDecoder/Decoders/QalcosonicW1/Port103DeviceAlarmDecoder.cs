using System;
using System.Collections.Generic;

namespace IoTPayloadDecoder.Decoders.QalcosonicW1
{
    public class Port103DeviceAlarmDecoder : IPayloadDecoder
    {
        private PayloadParser _parser;
        private DecodingResult _decodingResult;

        public dynamic Decode(string payloadString, bool compact)
        {
            if (string.IsNullOrWhiteSpace(payloadString))
            {
                throw new ArgumentException("Payload string cannot be empty", nameof(payloadString));
            }

            _parser = new PayloadParser(payloadString);
            _decodingResult = new DecodingResult(compact);

            DecodeAlarmPayload();

            return _decodingResult.FinishResult();
        }

        private void DecodeAlarmPayload()
        {
            DateTime time = _parser.GetUnixEpoch();
            byte status = _parser.GetUInt8();

            _decodingResult.AddResult("meterTimeUtc", time);
            _decodingResult.AddResult("status", status, Unit.Count);
            _decodingResult.AddResult("statusText", DecodeStatusText(status));
            _decodingResult.AddResult("statusFlags", DecodeStatusFlags(status));
        }

        private static string DecodeStatusText(byte status)
        {
            var flags = DecodeStatusFlags(status);

            if (flags.Length == 1)
            {
                return flags[0];
            }

            return string.Join(" + ", flags);
        }

        private static string[] DecodeStatusFlags(byte status)
        {
            var flags = new List<string>();

            if (status == 0x00)
            {
                return new[] { "No error" };
            }

            byte temporaryStatus = (byte)(status & 0xF0);

            switch (temporaryStatus)
            {
                case 0x10:
                    flags.Add("Empty pipe");
                    break;

                case 0x30:
                    flags.Add("Leakage");
                    break;

                case 0x70:
                    flags.Add("Negative flow / Backflow");
                    break;

                case 0x90:
                    flags.Add("Freeze");
                    break;

                case 0xB0:
                    flags.Add("Burst");
                    break;
            }

            if ((status & 0x08) != 0)
            {
                flags.Add("Permanent error");
            }

            if ((status & 0x04) != 0)
            {
                flags.Add("Low battery");
            }

            byte knownBits = 0x00;

            knownBits |= temporaryStatus;

            if ((status & 0x08) != 0)
            {
                knownBits |= 0x08;
            }

            if ((status & 0x04) != 0)
            {
                knownBits |= 0x04;
            }

            byte unknownBits = (byte)(status & ~knownBits);

            if (unknownBits != 0)
            {
                flags.Add($"Unknown flag 0x{unknownBits:X2}");
            }

            if (flags.Count == 0)
            {
                flags.Add($"Unknown status 0x{status:X2}");
            }

            return flags.ToArray();
        }
    }
}
