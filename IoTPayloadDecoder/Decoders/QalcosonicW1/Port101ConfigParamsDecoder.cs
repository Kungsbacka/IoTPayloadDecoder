using System;

namespace IoTPayloadDecoder.Decoders.QalcosonicW1
{
    public class Port101ConfigParamsDecoder : IPayloadDecoder
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

            DecodeConfigPayload();

            return _decodingResult.FinishResult();
        }

        private void DecodeConfigPayload()
        {
            DateTime time = _parser.GetUnixEpoch();
            byte status = _parser.GetUInt8();

        }
    }
}
