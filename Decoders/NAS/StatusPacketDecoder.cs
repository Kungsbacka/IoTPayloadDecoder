using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders.NAS
{
    public  class StatusPacketDecoder : IPayloadDecoder
    {
        public static int Port = 24;

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

            dynamic result = DecodeStatusPacket();
            result.errors = _errorList.ToArray();
            return result;
        }

        private dynamic DecodeStatusPacket()
        {
            dynamic packet = new ExpandoObject();

            packet.packet_type = Helpers.FormatAsValue("status_packet", _compact);
            uint epochRaw = _parser.GetUInt32(peek: true);
            packet.device_unix_epoch = Helpers.FormatAsValueAndRaw(_parser.GetUnixEpoch(), epochRaw, _compact);

            packet.status_field = new ExpandoObject();
            packet.status_field.dali_error_external = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            packet.status_field.dali_error_connection = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            packet.status_field.ldr_state = Helpers.FormatAsValue(_parser.GetBit(), _compact);

            _parser.GetBit(); // throw away bit

            packet.status_field.dig_state = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            packet.status_field.hardware_error = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            packet.status_field.firmware_error = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            packet.status_field.internal_relay_state = Helpers.FormatAsValue(_parser.GetBit(), _compact);

            packet.downlink_rssi = Helpers.FormatAsValueAndUnit(_parser.GetUInt8(), "dBm", _compact);
            packet.downlink_snr = Helpers.FormatAsValueAndUnit(_parser.GetInt8(), "dB", _compact);
            packet.mcu_temperature = Helpers.FormatAsValueAndUnit(_parser.GetInt8(), "\u00B0C", _compact);

            bool thr_sent = _parser.GetBit();
            bool ldr_sent = _parser.GetBit();

            packet.analog_interfaces = new ExpandoObject();
            packet.analog_interfaces.open_drain_out_state = Helpers.FormatAsValue(_parser.GetBit(), _compact);

            _parser.GetBit(); // throw away bit

            packet.analog_interfaces.voltage_alert_in_24h = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            packet.analog_interfaces.lamp_error_alert_in_24h = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            packet.analog_interfaces.power_alert_in_24h = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            packet.analog_interfaces.power_factor_alert_in_24h = Helpers.FormatAsValue(_parser.GetBit(), _compact);

            if (thr_sent)
            {
                packet.thr_value = Helpers.FormatAsValue(_parser.GetUInt8(), _compact);
            }

            if (ldr_sent)
            {
                packet.ldr_value = Helpers.FormatAsValue(_parser.GetUInt8(), _compact);
            }

            if (_parser.RemainingBits >= 5 * 8)
            {
                packet.profile = Helpers.DecodeProfile(_parser, _errorList, _compact);
                packet.profile.dimming_level = Helpers.FormatAsValueUnitAndMinMax(
                    _parser.GetUInt8(), "%", 0, 100, _compact);
            }

            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }
    }
}
