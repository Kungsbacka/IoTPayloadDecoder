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
            _decodingResult.AddResult("statusText", StatusDecoder.DecodeStatusText(status));
            _decodingResult.AddResult("statusFlags", StatusDecoder.DecodeStatusFlags(status));
        }
     
    }
}
