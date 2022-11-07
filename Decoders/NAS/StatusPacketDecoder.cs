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

            packet.packet_type = Helpers.WrapAsValue("status_packet", _compact);
            uint epochRaw = _parser.GetUInt32(peek: true);
            packet.device_unix_epoch = Helpers.WrapAsValueAndRaw(_parser.GetUnixEpoch(), epochRaw, _compact);

            packet.status_field = new ExpandoObject();
            packet.status_field.dali_error_external = Helpers.WrapAsValue(_parser.GetBit(), _compact);
            packet.status_field.dali_error_connection = Helpers.WrapAsValue(_parser.GetBit(), _compact);
            packet.status_field.ldr_state = Helpers.WrapAsValue(_parser.GetBit(), _compact);

            _parser.GetBit(); // throw away bit

            packet.status_field.dig_state = Helpers.WrapAsValue(_parser.GetBit(), _compact);
            packet.status_field.hardware_error = Helpers.WrapAsValue(_parser.GetBit(), _compact);
            packet.status_field.firmware_error = Helpers.WrapAsValue(_parser.GetBit(), _compact);
            packet.status_field.internal_relay_state = Helpers.WrapAsValue(_parser.GetBit(), _compact);

            packet.downlink_rssi = Helpers.WrapAsValueAndUnit(_parser.GetUInt8(), "dBm", _compact);
            packet.downlink_snr = Helpers.WrapAsValueAndUnit(_parser.GetInt8(), "dB", _compact);
            packet.mcu_temperature = Helpers.WrapAsValueAndUnit(_parser.GetInt8(), "\u00B0C", _compact);

            bool thr_sent = _parser.GetBit();
            bool ldr_sent = _parser.GetBit();

            packet.analog_interfaces = new ExpandoObject();
            packet.analog_interfaces.open_drain_out_state = Helpers.WrapAsValue(_parser.GetBit(), _compact);

            _parser.GetBit(); // throw away bit

            packet.analog_interfaces.voltage_alert_in_24h = Helpers.WrapAsValue(_parser.GetBit(), _compact);
            packet.analog_interfaces.lamp_error_alert_in_24h = Helpers.WrapAsValue(_parser.GetBit(), _compact);
            packet.analog_interfaces.power_alert_in_24h = Helpers.WrapAsValue(_parser.GetBit(), _compact);
            packet.analog_interfaces.power_factor_alert_in_24h = Helpers.WrapAsValue(_parser.GetBit(), _compact);

            if (thr_sent)
            {
                packet.thr_value = Helpers.WrapAsValue(_parser.GetUInt8(), _compact);
            }

            if (ldr_sent)
            {
                packet.ldr_value = Helpers.WrapAsValue(_parser.GetUInt8(), _compact);
            }

            if (_parser.RemainingBits >= 5 * 8)
            {
                packet.profile = DecodeProfile();
                packet.profile.dimming_level = Helpers.WrapAsValueUnitAndMinMax(
                    _parser.GetUInt8(), "%", 0, 100, _compact);
            }

            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }

        private dynamic DecodeProfile()
        {
            dynamic profile = new ExpandoObject();
            byte id = _parser.GetUInt8();
            byte version = _parser.GetUInt8();
            byte address = _parser.GetUInt8();
            profile.profile_id = id == 255
                ? Helpers.WrapAsValueAndRaw("no_profile", id, _compact)
                : Helpers.WrapAsValue(id, _compact);
            profile.profile_version = version > 240
                ? Helpers.WrapAsValueAndRaw("n/a", version, _compact)
                : Helpers.WrapAsValue(version, _compact);
            profile.profile_override = Helpers.WrapAsValueAndRaw(GetProfileOverrideReason(version), version, _compact);
            string daliAddress = Helpers.ConvertToDaliAddress(address);
            if (Helpers.IsInvalidDaliAddress(daliAddress))
            {
                _errorList.Add("Invalid DALI address");
            }
            profile.dali_address_short = Helpers.WrapAsValueAndRaw(daliAddress, address, _compact);
            byte active_days = _parser.GetUInt8(peek: true);
            var days = new List<string>();
            if (_parser.GetBit()) days.Add("holiday");
            if (_parser.GetBit()) days.Add("mon");
            if (_parser.GetBit()) days.Add("tue");
            if (_parser.GetBit()) days.Add("wed");
            if (_parser.GetBit()) days.Add("thu");
            if (_parser.GetBit()) days.Add("fri");
            if (_parser.GetBit()) days.Add("sat");
            if (_parser.GetBit()) days.Add("sun");
            profile.days_active = Helpers.WrapAsValueAndRaw(days, active_days, _compact);
            return profile;
        }

        private static string GetProfileOverrideReason(byte reason)
        {
            switch (reason)
            {
                case 246: return "driver_not_found";
                case 247: return "calendar_active";
                case 248: return "init_active";
                case 249: return "profile_not_active";
                case 250: return "ldr_active";
                case 251: return "thr_active";
                case 252: return "dig_active";
                case 253: return "manual_active";
                case 254: return "value_differ";
                case 255: return "unknown";
                default: return "none";
            };
        }

    }
}
