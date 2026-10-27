
using System;

namespace IoTPayloadDecoder.Decoders.QalcosonicW1
{
    public class Port100RegularDataDecoder : IPayloadDecoder
    {
        public dynamic Decode(string payloadString, bool compact)
        {
            if (string.IsNullOrWhiteSpace(payloadString))
                throw new ArgumentException("Payload string cannot be empty", nameof(payloadString));

            var parser = new PayloadParser(payloadString);
            var result = new DecodingResult(compact);

            int payloadLength = parser.RemainingBits / 8;

            switch (payloadLength)
            {
                case 37:
                    DecodeBasicPayload(parser, result);
                    break;

                case 47:
                    DecodeExtendedPayload(parser, result);
                    break;

                case 50:
                    DecodeExtendedPayload(parser, result);
                    result.AddWarning("Non-standard 50-byte payload decoded as Extended.");
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported Qalcosonic W1 payload length: {payloadLength} bytes.");
            }

            return result.FinishResult();
        }

        private static void DecodeBasicPayload(PayloadParser parser, DecodingResult result)
        {
            DecodeHeader(parser, result);

            // Six historical volume values
            for (int i = 1; i <= 6; i++)
            {
                result.AddResult(
                    $"pastVolume{i}",
                    parser.GetUInt32(),
                    Unit.Liter);
            }

            result.AddResult(
                "periodBetweenValues",
                parser.GetUInt32(),
                Unit.Second);
        }

        private static void DecodeExtendedPayload(PayloadParser parser, DecodingResult result)
        {
            DecodeHeader(parser, result);

            DateTime logTimeUtc = parser.GetUnixEpoch();
            uint logVolume = parser.GetUInt32();

            result.AddResult("logTimeUtc", logTimeUtc);
            result.AddResult("logVolume", logVolume, Unit.Liter);

            // Calculate historical volumes from 15 delta values
            uint volume = logVolume;
            var historicalVolumes = new uint[15];

            for (int i = 0; i < historicalVolumes.Length; i++)
            {
                ushort delta = parser.GetUInt16();
                volume = checked(volume + delta);
                historicalVolumes[i] = volume;
            }

            // Most recent historical value first
            for (int i = 0; i < historicalVolumes.Length; i++)
            {
                result.AddResult(
                    $"pastVolume{i + 1}",
                    historicalVolumes[historicalVolumes.Length - 1 - i],
                    Unit.Liter);
            }
        }

        private static void DecodeHeader(PayloadParser parser, DecodingResult result)
        {
            DateTime meterTimeUtc = parser.GetUnixEpoch();
            byte status = parser.GetUInt8();
            uint currentVolume = parser.GetUInt32();

            result.AddResult("meterTimeUtc", meterTimeUtc);
            result.AddResult("status", status, Unit.Count);
            result.AddResult("statusText", StatusDecoder.DecodeStatusText(status));
            result.AddResult("statusFlags", StatusDecoder.DecodeStatusFlags(status));
            result.AddResult("currentVolume", currentVolume, Unit.Liter);
        }
    }
}
