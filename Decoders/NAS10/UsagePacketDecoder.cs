using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders.NAS10
{
    public class UsagePacketDecoder : IPayloadDecoder
    {
        public static int Port = 25;

        private List<string> _errorList;
        private PayloadParser _parser;
        private bool _compact;

        public dynamic Decode(string payloadString, bool compact)
        {
            if (!Helpers.IsValidPayloadString(payloadString))
            {
                throw new ArgumentException("Payload string is not a valid hex string", nameof(payloadString));
            }

            _errorList = new List<string>();
            _parser = new PayloadParser(payloadString);
            _compact = compact;

            dynamic result = DecodeUsagePacket();
            result.errors = _errorList.ToArray();
            return result;
        }

        private dynamic DecodeUsagePacket()
        {
            dynamic packet = new ExpandoObject();
            packet.packet_type = Helpers.FormatAsValue("usage_packet", _compact);
            var consumption = new List<dynamic>();
            while (_parser.RemainingBits > 0)
            {
                consumption.Add(DecodeConsumption());
            }
            packet.consumption = consumption;
            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }

        private dynamic DecodeConsumption()
        {
            dynamic consumption = new ExpandoObject();
            byte address = _parser.GetUInt8();
            string daliAddress = Helpers.ConvertToDaliAddress(address, "internal_measurement");
            if (Helpers.IsInvalidDaliAddress(daliAddress))
            {
                _errorList.Add("Invalid DALI address");
            }
            consumption.dali_address_short = Helpers.FormatAsValueAndRaw(daliAddress, address, _compact);
            var bitFieldParser = new PayloadParser(_parser.GetUInt8());
            if (bitFieldParser.GetBit())
            {
                consumption.active_energy_total = Helpers.FormatAsValueAndUnit(_parser.GetUInt32(), "Wh", _compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.active_energy_instant = Helpers.FormatAsValueAndUnit(_parser.GetUInt16(), "W", _compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.load_side_energy_total = Helpers.FormatAsValueAndUnit(_parser.GetUInt32(), "Wh", _compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.load_side_energy_instant = Helpers.FormatAsValueAndUnit(_parser.GetUInt16(), "W", _compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.power_factor_instant = Helpers.FormatAsValue(_parser.GetUInt8() / 100, _compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.mains_voltage = Helpers.FormatAsValueAndUnit(_parser.GetUInt8(), "V", _compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.driver_operating_time = Helpers.FormatAsValueAndUnit(_parser.GetUInt32(), "s", _compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.lamp_on_time = Helpers.FormatAsValueAndUnit(_parser.GetUInt32(), address == 0xff ? "h" : "s", _compact);
            }
            return consumption;
        }
    }
}
