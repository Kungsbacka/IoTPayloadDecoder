using System.Dynamic;

namespace IoTPayloadDecoder
{
    public static class NasDecoder
    {
        public static dynamic Decode(string payloadString, int port, bool compact = false)
        {
            PayloadParser parser = new(payloadString);
            return port switch
            {
                24 => DecodeStatusPacket(parser, compact),
                25 => DecodeUsagePacket(parser, compact),
                99 => DecodeBootPacket(parser, compact),
                 _ => throw new InvalidOperationException("No decoder implemented for port"),
            };
        }

        private static dynamic DecodeBootPacket(PayloadParser parser, bool compact)
        {
            byte header = parser.GetUInt8();
            return header switch
            {
                0x00 => DecodeBootPacketValidPacket(parser, compact),
                0x13 => DecodeBootPacketConfigFailedPacket(parser, compact),
                   _ => throw new InvalidOperationException("Invalid boot packet header")
            };
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

            PayloadParser daliInfoParser = new(parser.GetUInt8());
            packet.dali_supply_state = DecodeDaliInfo(daliInfoParser.GetBits(7), compact);
            bool externalPower = daliInfoParser.GetBit();
            packet.dali_power_source = WrapAsValueAndRaw(externalPower ? "external" : "internal", externalPower, compact);

            PayloadParser driverParser = new(parser.GetUInt8());
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
            List<dynamic> consumption = new();
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
            PayloadParser bitFieldParser = new(parser.GetUInt8());
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
            List<string> days = new();
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

        private static dynamic? DecodeDaliInfo(int info, bool compact)
        {
            if (info < 0x70)
            {
                return WrapAsValueRawAndUnit(info, info, "V", compact);
            }
            return info switch
            {
                0x7E => WrapAsValueAndRaw("bus_high", info, compact),
                // err.warings.push('dali supply state: error')
                0x7F => WrapAsValueAndRaw("dali_error", info, compact),
                _ => WrapAsValueAndRaw("invalid_value", info, compact)
            };
        }

        private static IEnumerable<string> GetResetReason(byte resetReason)
        {
            List<string> reason = new();
            PayloadParser parser = new(resetReason);
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
            return reason switch
            {
                246 => "driver_not_found",
                247 => "calendar_active",
                248 => "init_active",
                249 => "profile_not_active",
                250 => "ldr_active",
                251 => "thr_active",
                252 => "dig_active",
                253 => "manual_active",
                254 => "value_differ",
                255 => "unknown",
                  _ => "none",
            };
        }

        private static string GetDeviceConfigName(byte config)
        {
            return config switch
            {
                0 => "dali",
                1 => "dali_nc",
                2 => "dali_no",
                3 => "analog_nc",
                4 => "analog_no",
                5 => "dali_analog_nc",
                6 => "dali_analog_no",
                7 => "dali_analog_nc_no",
                _ => throw new ArgumentException("Invalid device config", nameof(config))
            };
        }

        private static string GetErrorCodeText(byte reason)
        {
            return reason switch
            {
                0x00 => "n/a",
                0x01 => "n/a",
                0x02 => "unknown_fport",
                0x03 => "packet_size_short",
                0x04 => "packet_size_long",
                0x05 => "value_error",
                0x06 => "protocol_parse_error",
                0x07 => "reserved_flag_set",
                0x08 => "invalid_flag_combination",
                0x09 => "unavailable_feature_request",
                0x0A => "unsupported_header",
                0x0B => "unreachable_hw_request",
                0x0C => "address_not_available",
                0x0D => "internal_error",
                0x0E => "packet_size_error",
                 128 => "no_room",
                 129 => "id_seq_error",
                 130 => "destination_eror",
                 131 => "days_error",
                 132 => "step_count_error",
                 133 => "step_value_error",
                 134 => "step_unsorted",
                 135 => "days_overlap",
                   _ => "invalid_error_code"
            };
        }

        private static IEnumerable<string> GetOptionalFeatures(byte optionalFeatures)
        {
            List<string> features = new();
            PayloadParser featuresParser = new(optionalFeatures);
            featuresParser.GetBit();
            if (featuresParser.GetBit()) features.Add("thr");
            if (featuresParser.GetBit()) features.Add("dig");
            if (featuresParser.GetBit()) features.Add("ldr");
            if (featuresParser.GetBit()) features.Add("open_drain_out");
            if (featuresParser.GetBit()) features.Add("metering");
            if (featuresParser.GetBit()) features.Add("custom_request");
            return features;
        }

        private static string ConvertToDaliAddress(byte address, string? ff_str = null)
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

        private static dynamic? WrapAsValue<T>(T value, bool compact)
        {
            if (compact)
            {
                return (T)value;
            }
            dynamic result = new ExpandoObject();
            result.value = value;
            return result;
        }

        private static dynamic? WrapAsValueAndRaw<T1, T2>(T1 value, T2 raw, bool compact)
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

        private static dynamic? WrapAsValueAndUnit<T>(T value, string unit, bool compact)
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

        private static dynamic? WrapAsValueUnitAndMinMax<T>(T value, string unit, int min, int max, bool compact)
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

        private static dynamic? WrapAsValueRawAndUnit<T1, T2>(T1 value, T2 raw, string unit, bool compact)
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
