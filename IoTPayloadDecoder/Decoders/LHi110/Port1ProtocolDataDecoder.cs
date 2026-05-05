using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders.LHi110
{
    public class Port1ProtocolDataDecoder : PayloadDecoderBase, IPayloadDecoder
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

            DecodeProtocolData();

            return FinishResult();
        }

        private void DecodeProtocolData()
        {
            byte packetType = _parser.GetUInt8();
            byte index = _parser.GetUInt8();

            AddResult("packetType", packetType, Unit.Count);
            AddResult("index", index, Unit.Count);

            switch (packetType)
            {
                case 0x01:
                    AddResult("packetName", "Data Packet");
                    DecodeDataPacket(index);
                    break;

                case 0x02:
                    AddResult("packetName", "NACK Packet");
                    AddResult("nackIndex", index);
                    AddWarning($"Device returned NACK for index: 0x{index:X2}");
                    break;

                default:
                    AddResult("packetName", "Unknown");
                    AddWarning($"Unsupported packet type: 0x{packetType:X2}");
                    break;
            }
        }
        private void DecodeDataPacket(byte index)
        {
            switch (index)
            {
                //FW git SHA, läs 6 bytes text
                case 0x03:
                    AddResult("fwGitSha", _parser.GetString(6));
                    break;

                //CPU voltage, läs 2 bytes uint16 BE
                case 0x06:
                    AddResult("cpuVoltage", _parser.GetUInt16BE(), Unit.Millivolt);
                    break;

                //CPU temperature, läs 2 bytes uint16 BE
                case 0x0A:
                    ushort raw = _parser.GetUInt16BE();
                    var temperatureC = (raw / 100.0) - 50.0;
                    AddResult("cpuTemperatureRaw", raw);
                    AddResult("cpuTemperatureC", temperatureC, Unit.DegreeCelsius);
                    break;

                //status, läs 1 byte
                case 0x20:
                    byte status = _parser.GetUInt8();
                    AddResult("status", status, Unit.Count);
                    AddResult("statusText", status == 0 ? "No errors" : "Error flags set");
                    AddResult("watchdogResetOccurred", (status & 0x01) != 0);
                    AddResult("startupErrorOccurred", (status & 0x02) != 0);
                    break;

                //periodic interval, läs 2 bytes uint16 BE
                case 0x22:
                    AddResult("periodicInterval", _parser.GetUInt16BE(), Unit.Minute);
                    break;

                //report format, läs 1 byte
                case 0x23:
                    AddResult("reportFormat", _parser.GetUInt8(), Unit.Count);
                    break;

                default:
                    AddWarning($"Unsupported index: 0x{index:X2}");
                    break;
            }
        }
    }
}
