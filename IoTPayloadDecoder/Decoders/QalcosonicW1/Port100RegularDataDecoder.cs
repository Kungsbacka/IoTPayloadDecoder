using System;

namespace IoTPayloadDecoder.Decoders.QalcosonicW1
{
    public class Port100RegularDataDecoder : IPayloadDecoder
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

            DecodeBasicPayload();

            return _decodingResult.FinishResult();
        }

        private void DecodeBasicPayload()
        {
            DateTime time = _parser.GetUnixEpoch();
            byte status = _parser.GetUInt8();

            _decodingResult.AddResult("meterTimeUtc", time);
            _decodingResult.AddResult("status", status, Unit.Count);
            _decodingResult.AddResult("statusText", StatusDecoder.DecodeStatusText(status));
            _decodingResult.AddResult("statusFlags", StatusDecoder.DecodeStatusFlags(status));
            _decodingResult.AddResult("currentVolume", _parser.GetUInt32(), Unit.Liter);
            _decodingResult.AddResult("pastVolume1", _parser.GetUInt32(), Unit.Liter);
            _decodingResult.AddResult("pastVolume2", _parser.GetUInt32(), Unit.Liter);
            _decodingResult.AddResult("pastVolume3", _parser.GetUInt32(), Unit.Liter);
            _decodingResult.AddResult("pastVolume4", _parser.GetUInt32(), Unit.Liter);
            _decodingResult.AddResult("pastVolume5", _parser.GetUInt32(), Unit.Liter);
            _decodingResult.AddResult("pastVolume6", _parser.GetUInt32(), Unit.Liter);
            _decodingResult.AddResult("periodBetweenValues", _parser.GetUInt32(), Unit.Second);
        }
    }
}
