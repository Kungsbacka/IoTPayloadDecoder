using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders.LHi110
{
    public class Port1ProtocolDataDecoder : IPayloadDecoder
    {
        public const int Port = 1;

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

            dynamic result = DecodeProtocolData();
            result.warnings = _warnings.ToArray();
            return result;
        }

        private dynamic DecodeProtocolData()
        {
            dynamic data = new ExpandoObject();

            byte packetType = _parser.GetUInt8();
            byte index = _parser.GetUInt8();

            data.packetType = packetType;
            data.index = index;

            switch (packetType)
            {
                case 0x01:
                    data.packetName = "Data Packet";
                    DecodeDataPacket(data);
                    break;

                case 0x02:
                    data.packetName = "NACK Packet";
                    data.nackIndex = index;
                    _warnings.Add($"Device returned NACK for index: 0x{index:X2}");
                    break;

                default:
                    data.packetName = "Unknown";
                    _warnings.Add($"Unsupported packet type: 0x{packetType:X2}");
                    break;
            }

            return data;
        }
        private void DecodeDataPacket(dynamic data)
        {
            byte index = data.index;

            switch (index)
            {
                //FW git SHA, läs 6 bytes text
                case 0x03:
                    data.indexName = "FW git SHA";
                    data.fwGitSha = _parser.GetString(6);
                    break;

                //CPU voltage, läs 2 bytes uint16 BE
                case 0x06:
                    data.indexName = "CPU Voltage";
                    data.cpuVoltage_mV = _parser.GetUInt16BE();
                    break;

                //CPU temperature, läs 2 bytes uint16 BE
                case 0x0A:
                    data.indexName = "CPU Temperature";
                    ushort raw = _parser.GetUInt16BE();
                    data.cpuTemperatureRaw = raw;
                    data.cpuTemperature_C = (raw / 100.0) - 50.0;
                    break;

                //status, läs 1 byte
                case 0x20:
                    byte status = _parser.GetUInt8();

                    data.indexName = "Status";
                    data.status = status;
                    data.watchdogResetOccurred = (status & 0x01) != 0;
                    data.startupErrorOccurred = (status & 0x02) != 0;

                    if (status == 0)
                    {
                        data.statusMeaning = "No errors";
                    }
                    break;

                //periodic interval, läs 2 bytes uint16 BE
                case 0x22:
                    data.indexName = "Periodic Interval (minutes)";
                    data.periodicIntervalMinutes = _parser.GetUInt16BE();
                    break;

                //report format, läs 1 byte
                case 0x23:
                    data.indexName = "Report format";
                    data.reportFormat = _parser.GetUInt8();
                    break;

                default:
                    data.indexName = "Unknown";
                    _warnings.Add($"Unsupported index: 0x{data.index:X2}");
                    break;
            }
        }
    }
}
