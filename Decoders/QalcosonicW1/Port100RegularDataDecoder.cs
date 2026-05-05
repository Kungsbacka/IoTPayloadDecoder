using System;
using System.Collections.Generic;
using System.Dynamic;


namespace IoTPayloadDecoder.Decoders.QalcosonicW1
{
    public class Port100RegularDataDecoder : IPayloadDecoder
    {
        public const int Port = 100;

        private List<string> _warnings;
        private PayloadParser _parser;
        private bool _compact;
        private dynamic _result;

        public dynamic Decode(string payloadString, bool compact)
        {
            if (string.IsNullOrWhiteSpace(payloadString))
            {
                throw new ArgumentException("Payload string cannot be empty", nameof(payloadString));
            }

            _warnings = new List<string>();
            _parser = new PayloadParser(payloadString);
            _compact = compact;
            _result = new ExpandoObject();

            //DecodePeriodicData();

            _result.warnings = _warnings.ToArray();
            return _result;
        }

        private void DecodeBasicPayload()
        {
            DateTime time = _parser.GetUnixEpoch();
            byte status = _parser.GetUInt8();

            AddResult("meterTimeUtc", time, Unit.Unknown);
            AddResult("status", status, Unit.Count);
            AddResult("status", DecodeStatus(status), Unit.Unknown);
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

        private void AddResult<T>(string name, T value, Unit unit)
        {
            if (_compact)
            {
                ((IDictionary<string, object>)_result).Add(name, value);
            }
            else
            {
                dynamic tmp = new ExpandoObject();
                tmp.value = value;
                tmp.unit = unit.ToUnitString();

                //There has been cases with mutliple "debug" types resulting in exceptions
                if (!((IDictionary<string, object>)_result).ContainsKey(name))
                {
                    ((IDictionary<string, object>)_result).Add(name, tmp);
                }
            }
        }
    }
}
