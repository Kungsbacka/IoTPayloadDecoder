using System;

namespace IoTPayloadDecoder.Decoders.QalcosonicW1
{
    public class Port100RegularDataDecoder : PayloadDecoderBase, IPayloadDecoder
    {
        private PayloadParser _parser;

        public dynamic Decode(string payloadString, bool compact)
        {
            if (string.IsNullOrWhiteSpace(payloadString))
            {
                throw new ArgumentException("Payload string cannot be empty", nameof(payloadString));
            }

            _parser = new PayloadParser(payloadString);
            InitResult(compact);

            DecodeBasicPayload();

            return FinishResult();
        }

        private void DecodeBasicPayload()
        {
            DateTime time = _parser.GetUnixEpoch();
            byte status = _parser.GetUInt8();

            AddResult("meterTimeUtc", time);
            AddResult("status", status, Unit.Count);
            AddResult("statusText", DecodeStatus(status));
            AddResult("currentVolume", _parser.GetUInt32(), Unit.Liter);
            AddResult("pastVolume1", _parser.GetUInt32(), Unit.Liter);
            AddResult("pastVolume2", _parser.GetUInt32(), Unit.Liter);
            AddResult("pastVolume3", _parser.GetUInt32(), Unit.Liter);
            AddResult("pastVolume4", _parser.GetUInt32(), Unit.Liter);
            AddResult("pastVolume5", _parser.GetUInt32(), Unit.Liter);
            AddResult("pastVolume6", _parser.GetUInt32(), Unit.Liter);
            AddResult("periodBetweenValues", _parser.GetUInt32(), Unit.Second);
        }

        private static string DecodeStatus(byte status)
        {
            switch (status)
            {
                case 0x00: return "No error";
                case 0x04: return "Low battery";
                case 0x08: return "Permanent error";
                case 0x10: return "Empty pipe";
                case 0x30: return "Leakage";
                case 0x70: return "Backflow";
                case 0x90: return "Freeze";
                case 0xB0: return "Burst";
                default: return $"Unknown status 0x{status:X2}";
            }
        }
    }
}
