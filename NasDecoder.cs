using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder
{
    public static class NasDecoder
    {
        public static dynamic Decode(string payloadString, int port, bool compact = false)
        {
            var parser = new PayloadParser(payloadString);
            switch (port)
            {
                case 24:
                    return DecodeStatusPacket(parser, compact);
                case 25:
                    return DecodeUsagePacket(parser, compact);
                case 99:
                    return DecodeBootPacket(parser, compact);
                default:
                    throw new InvalidOperationException("No decoder implemented for port");
            }
        }

        private static dynamic DecodeBootPacket(PayloadParser parser, bool compact)
        {
            byte header = parser.GetUInt8();
            switch (header)
            {
                case 0x00:
                    return DecodeBootPacketValidPacket(parser, compact);
                case 0x13:
                    return DecodeBootPacketConfigFailedPacket(parser, compact);
                default:
                    throw new InvalidOperationException("Invalid boot packet header");
            }
        }

        private static dynamic DecodeBootPacketValidPacket(PayloadParser parser, bool compact)
        {
            dynamic packet = new ExpandoObject();
            packet.packet_type = WrapAsValue("boot_packet", compact);

            uint serial = parser.GetUInt32();
            packet.device_serial = WrapAsValue(serial.ToString("X8"), compact);

            byte major = parser.GetUInt8();
            byte minor = parser.GetUInt8();
            byte patch = parser.GetUInt8();
            packet.firmware_version = WrapAsValueAndRaw(
                string.Concat(major, ".", minor, ".", patch),
                ((major << 16) | (minor << 8) | patch).ToString(),
                compact
            );

            uint epochRaw = parser.GetUInt32(peek: true);
            packet.device_unix_epoch = WrapAsValueAndRaw(parser.GetUnixEpoch(), epochRaw, compact);

            byte deviceConfig = parser.GetUInt8();
            packet.device_config = WrapAsValueAndRaw(GetDeviceConfigName(deviceConfig), deviceConfig, compact);

            byte optionalFeatures = parser.GetUInt8();
            packet.optional_features = WrapAsValueAndRaw(GetOptionalFeatures(optionalFeatures), optionalFeatures, compact);

            var daliInfoParser = new PayloadParser(parser.GetUInt8());
            packet.dali_supply_state = DecodeDaliInfo(daliInfoParser.GetBits(7), compact);
            bool externalPower = daliInfoParser.GetBit();
            packet.dali_power_source = WrapAsValueAndRaw(externalPower ? "external" : "internal", externalPower, compact);

            var driverParser = new PayloadParser(parser.GetUInt8());
            packet.dali_addressed_driver_count = WrapAsValue(driverParser.GetBits(7), compact);
            packet.dali_unadressed_driver_found = WrapAsValue(driverParser.GetBit(), compact);

            if (parser.RemainingBits >= 8)
            {
                byte resetReason = parser.GetUInt8();
                packet.reset_reason = WrapAsValueAndRaw(GetResetReason(resetReason), resetReason, compact);
            }

            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }

        private static dynamic DecodeBootPacketConfigFailedPacket(PayloadParser parser, bool compact)
        {
            dynamic packet = new ExpandoObject();
            packet.packet_type = WrapAsValue("invalid_downlink_packet", compact);
            packet.packet_from_fport = WrapAsValue(parser.GetUInt8(), compact);
            byte errorCode = parser.GetUInt8();
            packet.parse_error_code = WrapAsValueAndRaw(GetErrorCodeText(errorCode), errorCode, compact);
            // err.errors.push("Config failed: " + error);
            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }

        private static dynamic DecodeUsagePacket(PayloadParser parser, bool compact)
        {
            dynamic packet = new ExpandoObject();
            packet.packet_type = WrapAsValue("usage_packet", compact);
            var consumption = new List<dynamic>();
            while (parser.RemainingBits > 0)
            {
                consumption.Add(DecodeConsumption(parser, compact));
            }
            packet.consumption = consumption;
            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }

        private static dynamic DecodeConsumption(PayloadParser parser, bool compact = false)
        {
            dynamic consumption = new ExpandoObject();
            byte address = parser.GetUInt8();
            string daliAddress = ConvertToDaliAddress(address, "internal_measurement");
            consumption.dali_address_short = WrapAsValueAndRaw(daliAddress, address, compact);
            var bitFieldParser = new PayloadParser(parser.GetUInt8());
            if (bitFieldParser.GetBit())
            {
                consumption.active_energy_total = WrapAsValueAndUnit(parser.GetUInt32(), "Wh", compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.active_energy_instant = WrapAsValueAndUnit(parser.GetUInt16(), "W", compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.load_side_energy_total = WrapAsValueAndUnit(parser.GetUInt32(), "Wh", compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.load_side_energy_instant = WrapAsValueAndUnit(parser.GetUInt16(), "W", compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.power_factor_instant = WrapAsValue(parser.GetUInt8() / 100, compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.mains_voltage = WrapAsValueAndUnit(parser.GetUInt8(), "V", compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.driver_operating_time = WrapAsValueAndUnit(parser.GetUInt32(), "s", compact);
            }
            if (bitFieldParser.GetBit())
            {
                consumption.lamp_on_time = WrapAsValueAndUnit(parser.GetUInt32(), address == 0xff ? "h" : "s", compact);
            }
            return consumption;
        }

        private static dynamic DecodeStatusPacket(PayloadParser parser, bool compact)
        {
            dynamic packet = new ExpandoObject();

            packet.packet_type = WrapAsValue("status_packet", compact);
            uint epochRaw = parser.GetUInt32(peek: true);
            packet.device_unix_epoch = WrapAsValueAndRaw(parser.GetUnixEpoch(), epochRaw, compact);

            packet.status_field = new ExpandoObject();
            packet.status_field.dali_error_external = WrapAsValue(parser.GetBit(), compact);
            packet.status_field.dali_error_connection = WrapAsValue(parser.GetBit(), compact);
            packet.status_field.ldr_state = WrapAsValue(parser.GetBit(), compact);

            parser.GetBit(); // throw away bit

            packet.status_field.dig_state = WrapAsValue(parser.GetBit(), compact);
            packet.status_field.hardware_error = WrapAsValue(parser.GetBit(), compact);
            packet.status_field.firmware_error = WrapAsValue(parser.GetBit(), compact);
            packet.status_field.internal_relay_state = WrapAsValue(parser.GetBit(), compact);

            packet.downlink_rssi = WrapAsValueAndUnit(parser.GetUInt8(), "dBm", compact);
            packet.downlink_snr = WrapAsValueAndUnit(parser.GetInt8(), "dB", compact);
            packet.mcu_temperature = WrapAsValueAndUnit(parser.GetInt8(), "\u00B0C", compact);

            bool thr_sent = parser.GetBit();
            bool ldr_sent = parser.GetBit();

            packet.analog_interfaces = new ExpandoObject();
            packet.analog_interfaces.open_drain_out_state = WrapAsValue(parser.GetBit(), compact);

            parser.GetBit(); // throw away bit

            packet.analog_interfaces.voltage_alert_in_24h = WrapAsValue(parser.GetBit(), compact);
            packet.analog_interfaces.lamp_error_alert_in_24h = WrapAsValue(parser.GetBit(), compact);
            packet.analog_interfaces.power_alert_in_24h = WrapAsValue(parser.GetBit(), compact);
            packet.analog_interfaces.power_factor_alert_in_24h = WrapAsValue(parser.GetBit(), compact);

            if (thr_sent)
            {
                packet.thr_value = WrapAsValue(parser.GetUInt8(), compact);
            }

            if (ldr_sent)
            {
                packet.ldr_value = WrapAsValue(parser.GetUInt8(), compact);
            }

            if (parser.RemainingBits >= 5 * 8)
            {
                packet.profile = DecodeProfile(parser, compact);
                packet.profile.dimming_level = WrapAsValueUnitAndMinMax(
                    parser.GetUInt8(), "%", 0, 100, compact);
            }

            dynamic result = new ExpandoObject();
            result.data = packet;
            return result;
        }

        private static dynamic DecodeProfile(PayloadParser parser, bool compact)
        {
            dynamic profile = new ExpandoObject();
            byte id = parser.GetUInt8();
            byte version = parser.GetUInt8();
            byte address = parser.GetUInt8();
            profile.profile_id = id == 255 ? WrapAsValueAndRaw("no_profile", id, compact) : WrapAsValue(id, compact);
            profile.profile_version = version > 240 ? WrapAsValueAndRaw("n/a", version, compact) : WrapAsValue(version, compact);
            profile.profile_override = WrapAsValueAndRaw(GetProfileOverrideReason(version), version, compact);
            string daliAddress = ConvertToDaliAddress(address);
            profile.dali_address_short = WrapAsValueAndRaw(daliAddress, address, compact);
            byte active_days = parser.GetUInt8(peek: true);
            var days = new List<string>();
            if (parser.GetBit()) days.Add("holiday");
            if (parser.GetBit()) days.Add("mon");
            if (parser.GetBit()) days.Add("tue");
            if (parser.GetBit()) days.Add("wed");
            if (parser.GetBit()) days.Add("thu");
            if (parser.GetBit()) days.Add("fri");
            if (parser.GetBit()) days.Add("sat");
            if (parser.GetBit()) days.Add("sun");
            profile.days_active = WrapAsValueAndRaw(days, active_days, compact);
            return profile;
        }

        private static dynamic DecodeDaliInfo(int info, bool compact)
        {
            if (info < 0x70)
            {
                return WrapAsValueRawAndUnit(info, info, "V", compact);
            }
            switch (info)
            {
                case 0x7E:
                    // err.warings.push('dali supply state: error')
                    return WrapAsValueAndRaw("bus_high", info, compact);
                case 0x7F:
                    return WrapAsValueAndRaw("dali_error", info, compact);
                default:
                    return WrapAsValueAndRaw("invalid_value", info, compact);
            }
        }

        private static IEnumerable<string> GetResetReason(byte resetReason)
        {
            var reason = new List<string>();
            var parser = new PayloadParser(resetReason);
            if (parser.GetBit()) reason.Add("reset_0");
            if (parser.GetBit()) reason.Add("watchdog_reset");
            if (parser.GetBit()) reason.Add("soft_reset");
            if (parser.GetBit()) reason.Add("reset_3");
            if (parser.GetBit()) reason.Add("reset_4");
            if (parser.GetBit()) reason.Add("reset_5");
            if (parser.GetBit()) reason.Add("reset_6");
            if (parser.GetBit()) reason.Add("reset_7");
            return reason;
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
                default:  return "none";
            };
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
                default:
                    throw new ArgumentException("Invalid device config", nameof(config));
            };
        }

        private static string GetErrorCodeText(byte reason)
        {
            switch (reason)
            {
               case   0: return "n/a";
               case   1: return "n/a";
               case   2: return "unknown_fport";
               case   3: return "packet_size_short";
               case   4: return "packet_size_long";
               case   5: return "value_error";
               case   6: return "protocol_parse_error";
               case   7: return "reserved_flag_set";
               case   8: return "invalid_flag_combination";
               case   9: return "unavailable_feature_request";
               case  10: return "unsupported_header";
               case  11: return "unreachable_hw_request";
               case  12: return "address_not_available";
               case  13: return "internal_error";
               case  14: return "packet_size_error";
               case 128: return "no_room";
               case 129: return "id_seq_error";
               case 130: return "destination_eror";
               case 131: return "days_error";
               case 132: return "step_count_error";
               case 133: return "step_value_error";
               case 134: return "step_unsorted";
               case 135: return "days_overlap";
               default:  return "invalid_error_code";
            };
        }

        private static IEnumerable<string> GetOptionalFeatures(byte optionalFeatures)
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

        private static string ConvertToDaliAddress(byte address, string ff_str = null)
        {
            if (address == 0xfe)
            {
                return "broadcast";
            }
            if (address == 0xff)
            {
                if (!string.IsNullOrEmpty(ff_str))
                {
                    return ff_str;
                }
                // err.errors.push("invalid DALI address");
                throw new ArgumentException("Invalid DALI address", nameof(address));
                // return "invalid";
            }
            if ((address & 0x01) > 0)
            {
                // err.errors.push("invalid DALI address");
                throw new ArgumentException("Invalid DALI address", nameof(address));
                // return "invalid";
            }
            if ((address & 0x80) > 0)
            {
                return string.Concat("group ", ((address >> 1) & 0xf));
            }
            return string.Concat("single " + ((address >> 1) & 0x3f));
        }

        private static dynamic WrapAsValue<T>(T value, bool compact)
        {
            if (compact)
            {
                return (T)value;
            }
            dynamic result = new ExpandoObject();
            result.value = value;
            return result;
        }

        private static dynamic WrapAsValueAndRaw<T1, T2>(T1 value, T2 raw, bool compact)
        {
            if (compact)
            {
                return (T1)value;
            }
            dynamic result = new ExpandoObject();
            result.value = value;
            result.raw = raw;
            return result;
        }

        private static dynamic WrapAsValueAndUnit<T>(T value, string unit, bool compact)
        {
            if (compact)
            {
                if (value == null)
                {
                    return null;
                }
                return string.Concat(value.ToString(), " ", unit);
            }
            dynamic result = new ExpandoObject();
            result.value = value;
            result.unit = unit;
            return result;
        }

        private static dynamic WrapAsValueUnitAndMinMax<T>(T value, string unit, int min, int max, bool compact)
        {
            dynamic result = new ExpandoObject();
            result.min = min;
            result.max = max;
            if (compact)
            {
                if (value == null)
                {
                    result.value = null;
                }
                else
                {
                    result.value = string.Concat(value.ToString(), " ", unit);
                }
                return result;
            }
            result.value = value;
            result.unit = unit;
            return result;
        }

        private static dynamic WrapAsValueRawAndUnit<T1, T2>(T1 value, T2 raw, string unit, bool compact)
        {
            if (compact)
            {
                if (value == null)
                {
                    return null;
                }
                return string.Concat(value.ToString(), " ", unit);
            }
            dynamic result = new ExpandoObject();
            result.value = value;
            result.raw = raw;
            result.unit = unit;
            return result;
        }
    }
}
