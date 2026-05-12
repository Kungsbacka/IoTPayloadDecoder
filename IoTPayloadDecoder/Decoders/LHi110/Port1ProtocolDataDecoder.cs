using System;

namespace IoTPayloadDecoder.Decoders.LHi110
{
    public class Port1ProtocolDataDecoder : IPayloadDecoder
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

            DecodeProtocolData();

            return _decodingResult.FinishResult();
        }

        private void DecodeProtocolData()
        {
            byte packetType = _parser.GetUInt8();
            byte index = _parser.GetUInt8();

            _decodingResult.AddResult("packetType", packetType, Unit.Count);
            _decodingResult.AddResult("index", index, Unit.Count);

            switch (packetType)
            {
                case 0x01:
                    _decodingResult.AddResult("packetName", "Data Packet");
                    DecodeDataPacket(index);
                    break;

                case 0x02:
                    _decodingResult.AddResult("packetName", "NACK Packet");
                    _decodingResult.AddResult("nackIndex", index);
                    _decodingResult.AddWarning($"Device returned NACK for index: 0x{index:X2}");
                    break;

                default:
                    _decodingResult.AddResult("packetName", "Unknown");
                    _decodingResult.AddWarning($"Unsupported packet type: 0x{packetType:X2}");
                    break;
            }
        }

        private void DecodeDataPacket(byte index)
        {
            switch (index)
            {
                //FW git SHA, läs 6 bytes text
                case 0x03:
                    _decodingResult.AddResult("fwGitSha", _parser.GetString(6));
                    break;

                //CPU voltage, läs 2 bytes uint16 BE
                case 0x06:
                    _decodingResult.AddResult("cpuVoltage", _parser.GetUInt16BE(), Unit.Millivolt);
                    break;

                //CPU temperature, läs 2 bytes uint16 BE
                case 0x0A:
                    ushort raw = _parser.GetUInt16BE();
                    var temperatureC = (raw / 100.0) - 50.0;
                    _decodingResult.AddResult("cpuTemperatureRaw", raw);
                    _decodingResult.AddResult("cpuTemperatureC", temperatureC, Unit.DegreeCelsius);
                    break;

                //status, läs 1 byte
                case 0x20:
                    byte status = _parser.GetUInt8();
                    _decodingResult.AddResult("status", status, Unit.Count);
                    _decodingResult.AddResult("statusText", status == 0 ? "No errors" : "Error flags set");
                    _decodingResult.AddResult("watchdogResetOccurred", (status & 0x01) != 0);
                    _decodingResult.AddResult("startupErrorOccurred", (status & 0x02) != 0);
                    break;

                //periodic interval, läs 2 bytes uint16 BE
                case 0x22:
                    _decodingResult.AddResult("periodicInterval", _parser.GetUInt16BE(), Unit.Minute);
                    break;

                //report format, läs 1 byte
                case 0x23:
                    _decodingResult.AddResult("reportFormat", _parser.GetUInt8(), Unit.Count);
                    break;

                default:
                    _decodingResult.AddWarning($"Unsupported index: 0x{index:X2}");
                    break;
            }
        }
    }
}
