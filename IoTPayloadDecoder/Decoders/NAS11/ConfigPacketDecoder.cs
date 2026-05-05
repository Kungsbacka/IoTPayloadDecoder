using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Text;

namespace IoTPayloadDecoder.Decoders.NAS11
{
    public class ConfigPacketDecoder : IPayloadDecoder
    {
        public static int Port = 50;

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

            dynamic result = new ExpandoObject();
            byte header = _parser.GetUInt8();
            switch (header)
            {
                case 0x01: //Klar
                    result = DecodeLdrConfig();
                    break;
                case 0x03: //Klar
                    result = DecodeDigConfig();     
                    break;
                case 0x05: //Klar
                    result.packet_type =  Helpers.FormatAsValue("open_drain_out_config_packet", _compact);
                    var list = new List<dynamic>();
                    while (_parser.RemainingBits > 0)
                    {
                        list.Add(DecodeOdRelaySwStep());    
                    }
                    result.switching_steps = list.ToArray();
                    break;
                case 0x07: //Klar
                    result = DecodeStatusConfig();
                    break;
                case 0x09: //Se kommentar
                    result = DecodeTimeConfig();
                    break;
				case 0x0B: //Klar
                    result = DecodeUsageConfig();
                    break;
                case 0x0D: //Klar
                    result = DecodeBootDelayConfig();
                    break;
                case 0x15: //Klar
                    result = DecodeLedConfig();
                    break;
                case 0x16: //Klar
                    result = DecodeMeteringAlertConfig();
                    break;
                case 0x20: //Klar
                    result = DecodeCalendarConfigV11();
                    break;
                case 0x21: //Klar
                    result = DecodeProfileConfig();
                    break;
                case 0x22: //Klar
                    result = DecodeFadeConfig();
                    break;
                case 0x23: //Klar
                    result = DecodeHolidayConfig();
                    break;
                case 0x24: //Klar
                    result = DecodeDaliMonitorConfig();
                    break;
                case 0x25: //Klar
                    result = DecodeFallbackDimConfig();
                    break;
                case 0x26: //Klar
                    result = DecodeLocationConfigV11();
                    break;
                case 0x52: //Se kommentar
                    result = DecodeMulticastConfig();
                    break;
                case 0x53: //Klar
                    result = DecodeMulticastFcntConfig();
                    break;
                case 0xFE: //Klar?
                    result.packet_type = Helpers.FormatAsValue("chained_config_packet", _compact);
                    result.payloads = new List<dynamic>();
                    while (_parser.RemainingBits > 0)
                    {
                        string remaining_payload = _parser.GetHexString(_parser.RemainingBits * 8);
                        result.payloads.Add(Decode(remaining_payload, _compact));    //Se över
                    }
                    break;
                case 0xFF: //Klar
                    result = DecodeClearConfig();
                    break;
                default:
                    _errorList.Add("invalid_header");
                    break;
            }
            
            result.errors = _errorList.ToArray();
            return result;
        }

        private dynamic DecodeMulticastFcntConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("multicast_fcnt_config_packet", _compact);

            result.multicast_device = Helpers.FormatAsValue(_parser.GetUInt8(), _compact);
            result.multicast_fcnt = Helpers.FormatAsValue(_parser.GetUInt32(), _compact);

            return result;
        }

        private dynamic DecodeFallbackDimConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("fallback_dim_config_packet", _compact);
            result.fallback_dimming_level = Helpers.FormatAsValueAndUnit(_parser.GetUInt8(), "%", _compact);

            return result;
        }

        private dynamic DecodeDaliMonitorConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("dali_monitor_config_packet", _compact);

            result.send_dali_alert = _parser.GetBit();
            result.correct_dali_dimming_level = _parser.GetBit();
            result.periodic_bus_scan_enabled = _parser.GetBit();

            ushort interval = _parser.GetUInt16();
            result.monitoring_interval = Helpers.FormatAsValueRawAndUnit(
                value: interval == 0 ? "disabled" : interval.ToString(),
                raw: interval,
                unit: interval == 0 ? "" : "s",
                _compact
            );

            return result;
        }

        private dynamic DecodeHolidayConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("holiday_config_packet", _compact);
            result.holidays = new List<dynamic>();
            
            for (int i = 0; i < _parser.GetUInt8(); i++)
            {
                byte month = _parser.GetUInt8();
                byte day = _parser.GetUInt8();
                dynamic holiday = Helpers.FormatAsValueAndRaw(
                    value: $"{month}/{day}",
                    raw: month << 8 | day,
                    _compact
                );
                result.holidays.Add(holiday);
            }

            return result;
        }

        private dynamic DecodeFadeConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("fade_config_packet", _compact);
            
            List<double> lookup = new List<double>()
            { 0.5, 0.71, 1.0, 1.41, 2.0, 2.83, 4.0, 5.66, 8.0, 11.31, 16.0, 22.63, 32.0, 45.25, 64.0, 90.51 };
            byte fade_dur = _parser.GetUInt8();
            string unit = "";
            string value = "";
            if (fade_dur == 255)
            {
                value = "ignore";
            }
            else if (fade_dur >= 16)
            {
                value = "invalid_fade";
                _errorList.Add("invalid_fade");
            }
            else
            {
                value = lookup[fade_dur].ToString();
                unit = "s";
            }
            result.fade_duration = Helpers.FormatAsValueRawAndUnit(value, fade_dur, unit, _compact);
            return result;
        }

        private dynamic DecodeLdrConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = "ldr_input_config_packet";
            byte high = _parser.GetUInt8();
            byte low = _parser.GetUInt8();
            result.ldr_off_threshold_high = Helpers.FormatAsValueAndRaw(
                value: high == 0xFF ? "disabled" : high.ToString(),
                raw: high,
                _compact
            );
            result.ldr_on_threshold_low = Helpers.FormatAsValueAndRaw(
                value: low == 0xFF ? "disabled" : low.ToString(),
                raw: low,
                _compact
            );
            _parser.GetBits(2); // Throw away two bits
            result.trigger_alert_enabled = _parser.GetBit();
            return result;
        }

        private dynamic DecodeDigConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = "dig_input_config_packet";
            ushort time = _parser.GetUInt16();
            result.light_on_duration = Helpers.FormatAsValueRawAndUnit(
                value: time == 0xFFFF ? "dig_input_disabled" : time.ToString(),
                raw: time,
                unit: "s",
                _compact
            );
            _parser.GetBit(); // Throw away bit
            bool edge = _parser.GetBit();
            result.switch_transition = Helpers.FormatAsValueAndRaw(
                value: edge ? "enabled" : "disabled",
                raw: edge,
                _compact
            );
            result.switch_trigger_alert_enabled = _parser.GetBit();

            byte address = _parser.GetUInt8();
            string daliAddress = Helpers.ConvertToDaliAddress(address, null);
            if (Helpers.IsInvalidDaliAddress(daliAddress))
            {
                _errorList.Add("Invalid DALI address");
            }
            result.dali_address_short = daliAddress;
            result.dimming_level = Helpers.FormatAsValueAndUnit(_parser.GetUInt8(), "%", _compact);
            return result;
        }

        private dynamic DecodeOdRelaySwStep()
        {
            dynamic result = new ExpandoObject();

            // This will "overflow" into days, but the original implementation
            // only decode as hh:mm so we do the same.
            int minutes = _parser.GetUInt8() * 10;
            int h = minutes / 60;
            int m = minutes - h * 10;
            result.step_time = Helpers.FormatAsValueRawAndUnit(
                value: string.Format("{0:D2}:{1:D2}", h, m),
                raw: minutes,
                unit: "hh:mm",
                _compact
            );
            result.open_drain_out_state = _parser.GetUInt8() != 0;
            return result;
        }

        private dynamic DecodeStatusConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("status_config_packet", _compact);
            result.status_interval = Helpers.FormatAsValueAndUnit(_parser.GetUInt32(), "s", _compact);
            return result;
        }

        private dynamic DecodeLocationConfigV11()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("location_config_packet", _compact);

            byte adress_length = _parser.GetUInt8();
            result.latitude = Helpers.FormatAsValueAndUnit(_parser.GetInt32() / 10000000.0, "\u00B0", _compact);
            result.longitude = Helpers.FormatAsValueAndUnit(_parser.GetInt32() / 10000000.0, "\u00B0", _compact);

            result.adress = Helpers.FormatAsValue(_parser.GetString(adress_length), _compact);

            return result;

        }

        private dynamic DecodeTimeConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("time_config_packet", _compact);

            var epochRaw = _parser.GetUInt32(peek: true); //Ska peek vara true eller false här?
            result.device_unix_epoch = Helpers.FormatAsValueAndRaw(_parser.GetUnixEpoch(), epochRaw, _compact);

            return result;
        }
        private dynamic DecodeUsageConfig()
        {
            dynamic result = new ExpandoObject();

            result.packet_type = Helpers.FormatAsValue("usage_config_packet", _compact);
            result.usage_interval = Helpers.FormatAsValueAndUnit(_parser.GetUInt32(), "s", _compact);
            byte volt = _parser.GetUInt8();

            if (volt != 0xFF)
            {
                result.mains_voltage = Helpers.FormatAsValueAndUnit(volt, "V", _compact);
            }
       
            return result;
        }
        private dynamic DecodeBootDelayConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("boot_delay_config_packet", _compact);

            result.boot_delay_range = Helpers.FormatAsValueAndUnit(
                    _parser.RemainingBits == 1 ? _parser.GetUInt8() : _parser.GetUInt16(),
                    "s",
                    _compact
                );

            return result;
        }
        private dynamic DecodeLedConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("onboard_led_config_packet", _compact);
            result.status_led_enabled = Helpers.FormatAsValue(_parser.GetBit(), _compact);
            return result;
        }

        private dynamic AlertParamConfig(ushort value, string unit)
        {
            return Helpers.FormatAsValueRawAndUnit(
                value: value == 0xFFFF ? "alert_off" : value.ToString(),
                raw: value,
                unit,
                _compact);
        }

        private dynamic DecodeMeteringAlertConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("metering_alert_config_packet", _compact);
            var header = _parser.GetUInt8();
            if (header != 0x01)
            {
                _errorList.Add("Invalid header");
                return result;
            }

            result.min_power = AlertParamConfig(_parser.GetUInt16(), "W");
            result.max_power = AlertParamConfig(_parser.GetUInt16(), "W");
            result.min_voltage = AlertParamConfig(_parser.GetUInt16(), "V");
            result.max_voltage = AlertParamConfig(_parser.GetUInt16(), "V");

            byte minPf = _parser.GetUInt8();
            result.min_power_factor = Helpers.FormatAsValueAndRaw(
                value: minPf == 0xFF ? "alert_off" : (minPf / 100.0).ToString(),
                raw: minPf,
                _compact
            );

            return result;
        }
        private dynamic DecodeMulticastConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("multicast_config_packet", _compact);

            byte dev = _parser.GetUInt8();

            if (dev > 3)
            {
                _errorList.Add("invalid_multicast_device");
                return result;
            }

            result.multicast_device = Helpers.FormatAsValue(dev, _compact);
            
            //TODO
            result.devaddr = Helpers.FormatAsValue(_parser.GetHexString(4), _compact); //Reverse
            result.nwkskey = Helpers.FormatAsValue(_parser.GetHexString(16), _compact);
            result.appskey = Helpers.FormatAsValue(_parser.GetHexString(16), _compact);
            

            return result;
        }
        private dynamic DecodeClearConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("clear_config_packet", _compact);

            byte type = _parser.GetUInt8();
            switch (type)
            {
                case 0x01:
                    result.reset_target = Helpers.FormatAsValue("ldr_input_config", _compact);
                    break;
                case 0x03:
                    result.reset_target = Helpers.FormatAsValue("dig_input_config", _compact);
                    break;
                case 0x21:
                    result.reset_target = Helpers.FormatAsValue("profile_config", _compact);
                    byte addr = _parser.GetUInt8();
                    result.address = Helpers.FormatAsValueAndRaw(
                        value: Helpers.ConvertToDaliAddress(addr, "all_profiles"),
                        raw: addr,
                        _compact);
                    break;
                case 0x23:
                    result.reset_target = Helpers.FormatAsValue("holiday_config", _compact);
                    break;
                case 0x52:
                    result.reset_target = Helpers.FormatAsValue("multicast_config", _compact);
                    byte device = _parser.GetUInt8();
                    result.multicast_device = Helpers.FormatAsValueAndRaw(
                        value: device == 0xFF ? "all_multicast_devices" : $"multicast_device_{device}",
                        raw: device,
                        _compact
                    );
                    break;
                case 0xFF:
                    result.reset_target = Helpers.FormatAsValue("factory_reset", _compact);
                    uint serial = _parser.GetUInt32();
                    result.device_serial = Helpers.FormatAsValue(serial.ToString("X8"), _compact);
                    break;
                default:
                    _errorList.Add("invalid_clear_config_target");
                    break;
            }

            return result;
        }
        private dynamic DecodeZenithStep(sbyte angle, byte dim)
        {
            dynamic step = new ExpandoObject();
            
            step.zenith_angle = Helpers.FormatAsValueRawAndUnit(
                value: (angle / 6 + 90).ToString("N2"),
                raw: angle,
                unit: "°",
                _compact
            );

            step.dimming_level = Helpers.FormatAsValueRawAndUnit(
                value: dim == 0xFF ? "disabled" : dim.ToString(),
                raw: dim,
                unit: dim == 0xFF ? "" : "%",
                _compact
            );
            return step;
        }
        private dynamic DecodeCalendarConfigV11()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("calendar_config_packet", _compact);
            
            byte sunrise = _parser.GetBits(4);
            byte sunset = _parser.GetBits(4);

            _parser.GetUInt8(); //Throw away bit

            result.latitude = Helpers.FormatAsValueAndUnit(_parser.GetInt16() / 100, "°", _compact);
            result.longitude = Helpers.FormatAsValueAndUnit(_parser.GetInt16() / 100, "°", _compact);

            result.sunrise_steps = new List<dynamic>();
            result.sunset_steps = new List<dynamic>();

            for (int i = 0; i < sunrise; i++)
            {
                sbyte angle = _parser.GetInt8();
                byte dim = _parser.GetUInt8();
                result.sunrise_steps.Add(DecodeZenithStep(angle, dim));
            }
            for (int i = 0; i < sunset; i++)
            {
                sbyte angle = _parser.GetInt8();
                byte dim = _parser.GetUInt8();
                result.sunrise_steps.Add(DecodeZenithStep(angle, dim));
            }
           
            return result;
        }
        private dynamic DecodeDimmingStep()
        {
            dynamic result = new ExpandoObject();

            int minutes = _parser.GetUInt8() * 10;
            int h = minutes / 60;
            int m = minutes - h * 10;
            result.step_time = Helpers.FormatAsValueRawAndUnit(
                value: string.Format("{0:D2}:{1:D2}", h, m),
                raw: minutes,
                unit: "hh:mm",
                _compact
            );

            byte dim = _parser.GetUInt8();
            result.dimming_level = Helpers.FormatAsValueRawAndUnit(
                value: dim == 0xFF ? "inactive" : dim.ToString(),
                raw: dim,
                unit: dim == 0xFF ? "" : "%",
                _compact
            );

            return result;
        }
        private dynamic DecodeProfileConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("profile_config_packet", _compact);
            byte id = _parser.GetUInt8();
            result.profile_id = Helpers.FormatAsValueAndRaw(
                value: id == 255 ? "no_profile" : id.ToString(),
                raw: id,
                _compact
            );
            byte length = _parser.GetUInt8();
            byte address = _parser.GetUInt8();

            result.address = Helpers.ConvertToDaliAddress(address);

            byte active_days = _parser.GetUInt8();
            List<dynamic> days = new List<dynamic>();

            if (_parser.GetBit()) { days.Add("holiday"); }
            if (_parser.GetBit()) { days.Add("mon"); }
            if (_parser.GetBit()) { days.Add("tue"); }
            if (_parser.GetBit()) { days.Add("wed"); }
            if (_parser.GetBit()) { days.Add("thu"); }
            if (_parser.GetBit()) { days.Add("fri"); }
            if (_parser.GetBit()) { days.Add("sat"); }
            if (_parser.GetBit()) { days.Add("sun"); }

            result.days_active = Helpers.FormatAsValueAndRaw(
                value: days.ToArray(),
                raw: active_days,
                _compact
            );
            _parser.GetUInt8(); //Throw away bit

            result.dimming_steps = new List<dynamic>();
            for (int i = 0; i < length; i++)
            {
                result.dimming_steps.Add(DecodeDimmingStep());
            }
            
            return result;
        }

    }
}