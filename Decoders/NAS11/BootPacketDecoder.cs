using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace IoTPayloadDecoder.Decoders.NAS11
{
    public class BootPacketDecoder : IPayloadDecoder
    {
        public static int Port = 99;

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

            byte header = _parser.GetUInt8();
            dynamic result;
            switch (header)
            {
                case 0x00:
                    result = DecodeBootPacketValidPacket();
                    break;
                case 0x13:
                    result = DecodeBootPacketConfigFailedPacket();
                    break;
                default:
                    throw new PayloadDecodingException("Invalid boot packet header");
            }
            result.errors = _errorList.ToArray();
            return result;
        }

        private dynamic DecodeBootPacketValidPacket()
        {
            dynamic packet = new ExpandoObject();
            packet.packet_type = Helpers.FormatAsValue("boot_packet", _compact);

            uint serial = _parser.GetUInt32();
            packet.device_serial = Helpers.FormatAsValue(serial.ToString("X8"), _compact);

            byte major = _parser.GetUInt8();
            byte minor = _parser.GetUInt8();
            byte patch = _parser.GetUInt8();
            packet.firmware_version = Helpers.FormatAsValueAndRaw(
                string.Concat(major, ".", minor, ".", patch),
                ((major << 16) | (minor << 8) | patch).ToString(),
                _compact
            );

            uint epochRaw = _parser.GetUInt32(peek: true);
            packet.device_unix_epoch = Helpers.FormatAsValueAndRaw(_parser.GetUnixEpoch(), epochRaw, _compact);

            byte deviceConfig = _parser.GetUInt8();
            packet.device_config = Helpers.FormatAsValueAndRaw(GetDeviceConfigName(deviceConfig), deviceConfig, _compact);

            byte optionalFeatures = _parser.GetUInt8();
            var featureList = GetOptionalFeatures(optionalFeatures);
            if (featureList.Count == 1)
            {
                packet.optional_features = Helpers.FormatAsValueAndRaw(featureList[0], optionalFeatures, _compact);
            }
            else
            {
                packet.optional_features = Helpers.FormatAsValueAndRaw(featureList, optionalFeatures, _compact);
            }
            var daliInfoParser = new PayloadParser(_parser.GetUInt8());
            packet.dali_supply_state = DecodeDaliInfo(daliInfoParser.GetBits(7));
            bool externalPower = daliInfoParser.GetBit();
            packet.dali_power_source = Helpers.FormatAsValueAndRaw(externalPower ? "external" : "internal", externalPower, _compact);

            var driverParser = new PayloadParser(_parser.GetUInt8());
            packet.dali_addressed_driver_count = Helpers.FormatAsValue(driverParser.GetBits(7), _compact);
            packet.dali_unadressed_driver_found = Helpers.FormatAsValue(driverParser.GetBit(), _compact);

            if (_parser.RemainingBits >= 8)
            {
                byte resetReason = _parser.GetUInt8();
                packet.reset_reason = Helpers.FormatAsValueAndRaw(GetResetReason(resetReason), resetReason, _compact);
            }

            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }

        private dynamic DecodeBootPacketConfigFailedPacket()
        {
            dynamic packet = new ExpandoObject();
            packet.packet_type = Helpers.FormatAsValue("invalid_downlink_packet", _compact);
            packet.packet_from_fport = Helpers.FormatAsValue(_parser.GetUInt8(), _compact);
            byte errorCode = _parser.GetUInt8();
            string errorText = GetErrorCodeText(errorCode);
            packet.parse_error_code = Helpers.FormatAsValueAndRaw(errorText, errorCode, _compact);
            _errorList.Add($"Config failed: {errorText}");
            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }

        private dynamic DecodeDaliInfo(int info)
        {
            if (info < 0x70)
            {
                return Helpers.FormatAsValueRawAndUnit(info, info, "V", _compact);
            }
            switch (info)
            {
                case 0x7E:
                    // err.warings.push('dali supply state: error')
                    return Helpers.FormatAsValueAndRaw("bus_high", info, _compact);
                case 0x7F:
                    return Helpers.FormatAsValueAndRaw("dali_error", info, _compact);
                default:
                    return Helpers.FormatAsValueAndRaw("invalid_value", info, _compact);
            }
        }

        private IList<string> GetOptionalFeatures(byte optionalFeatures)
        {
            var features = new List<string>();
            var featuresParser = new PayloadParser(optionalFeatures);
            featuresParser.GetBit();
            if (featuresParser.GetBit()) features.Add("thr");
            if (featuresParser.GetBit()) features.Add("dig");
            if (featuresParser.GetBit()) features.Add("ldr");
            if (featuresParser.GetBit()) features.Add("open_drain_out");
            if (featuresParser.GetBit()) features.Add("metering");
            if (featuresParser.GetBit()) features.Add("custom_request");
            return features;
        }

        private IEnumerable<string> GetResetReason(byte resetReason)
        {
            var reason = new List<string>();
            var reasonParser = new PayloadParser(resetReason);
            if (reasonParser.GetBit()) reason.Add("reset_0");
            if (reasonParser.GetBit()) reason.Add("watchdog_reset");
            if (reasonParser.GetBit()) reason.Add("soft_reset");
            if (reasonParser.GetBit()) reason.Add("reset_3");
            if (reasonParser.GetBit()) reason.Add("reset_4");
            if (reasonParser.GetBit()) reason.Add("reset_5");
            if (reasonParser.GetBit()) reason.Add("reset_6");
            if (reasonParser.GetBit()) reason.Add("reset_7");
            return reason;
        }

        private static string GetDeviceConfigName(byte config)
        {
            switch (config)
            {
                case 0: return "dali";
                case 1: return "dali_nc";
                case 2: return "dali_no";
                case 3: return "analog_nc";
                case 4: return "analog_no";
                case 5: return "dali_analog_nc";
                case 6: return "dali_analog_no";
                case 7: return "dali_analog_nc_no";
                default: throw new PayloadDecodingException("Invalid device config");
            };
        }

        private static string GetErrorCodeText(byte reason)
        {
            switch (reason)
            {
                case 0: return "n/a";
                case 1: return "n/a";
                case 2: return "unknown_fport";
                case 3: return "packet_size_short";
                case 4: return "packet_size_long";
                case 5: return "value_error";
                case 6: return "protocol_parse_error";
                case 7: return "reserved_flag_set";
                case 8: return "invalid_flag_combination";
                case 9: return "unavailable_feature_request";
                case 10: return "unsupported_header";
                case 11: return "unreachable_hw_request";
                case 12: return "address_not_available";
                case 13: return "internal_error";
                case 14: return "packet_size_error";
                case 128: return "no_room";
                case 129: return "id_seq_error";
                case 130: return "destination_eror";
                case 131: return "days_error";
                case 132: return "step_count_error";
                case 133: return "step_value_error";
                case 134: return "step_unsorted";
                case 135: return "days_overlap";
                default: return "invalid_error_code";
            };
        }
    }
}
