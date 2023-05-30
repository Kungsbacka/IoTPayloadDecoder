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
                case 0x01:
                    result = DecodeLdrConfig();
                    break;
                case 0x03:
                    result = DecodeDigConfig();
                    break;
                case 0x05:
                    result.packet_type =  Helpers.FormatAsValue("open_drain_out_config_packet", _compact);
                    var list = new List<dynamic>();
                    while (_parser.RemainingBits > 0)
                    {
                        list.Add(DecodeDigConfig());
                    }
                    result.switching_steps = list.ToArray();
                    break;
                case 0x06:
                    result = DecodeCalendarConfig();
                    break;
                case 0x07:
                    result = DecodeStatusConfig();
                    break;
                case 0x08:
                    // decodeProfileConfig(dataView, result, err);
                    throw new NotImplementedException("decodeProfileConfig");
                case 0x09:
					// decodeTimeConfig(dataView, result);
					throw new NotImplementedException("decodeTimeConfig");
				case 0x0A:
					// decodeLegacyDefaultsConfig(dataView, result);
					throw new NotImplementedException("decodeLegacyDefaultsConfig");
				case 0x0B:
                    // decodeUsageConfig(dataView, result);
                    throw new NotImplementedException("decodeUsageConfig");
                case 0x0C:
                    // decodeHolidayConfig(dataView, result);
                    throw new NotImplementedException("decodeHolidayConfig");
                case 0x0D:
                    // decodeBootDelayConfig(dataView, result);
                    throw new NotImplementedException("decodeBootDelayConfig");
                case 0x0E:
                    // decodeDefaultsConfig(dataView, result);
                    throw new NotImplementedException("decodeDefaultsConfig");
                case 0x13:
                    // decodeLocationConfig(dataView, result);
                    throw new NotImplementedException("decodeLocationConfig");
                case 0x15:
                    // decodeLedConfig(dataView, result);
                    throw new NotImplementedException("decodeLedConfig");
                case 0x16:
                    // decodeMeteringAlertConfig(dataView, result, err);
                    throw new NotImplementedException("decodeMeteringAlertConfig");
                case 0x52:
                    // decodeMulticastConfig(dataView, result, err);
                    throw new NotImplementedException("decodeMulticastConfig");
                case 0xFF:
                    // decodeClearConfig(dataView, result, err);
                    throw new NotImplementedException("decodeClearConfig");
                default:
                    _errorList.Add("invalid_header");
                    break;
            }
            
            result.errors = _errorList.ToArray();
            return result;
        }

        private dynamic DecodeLdrConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = "ldr_config_packet";
            byte high = _parser.GetUInt8();
            byte low = _parser.GetUInt8();
            result.switch_threshold_high = Helpers.FormatAsValueAndRaw(
                value: high == 0xFF ? "disabled" : high.ToString(),
                raw: high,
                _compact
            );
            result.switch_threshold_low = Helpers.FormatAsValueAndRaw(
                value: low == 0xFF ? "disabled" : low.ToString(),
                raw: low,
                _compact
            );
            _parser.GetBits(2); // Throw away two bits
            result.switch_trigger_alert_enabled = _parser.GetBit();
            return result;
        }

        private dynamic DecodeDigConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = "dig_config_packet";
            ushort time = _parser.GetUInt16();
            result.switch_time = Helpers.FormatAsValueRawAndUnit(
                value: time == 0xFF ? "disabled" : time.ToString(),
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

        private dynamic DecodeCalendarConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = "calendar_config_packet";
            sbyte sunrise = _parser.GetInt8();
            sbyte sunset = _parser.GetInt8();
            bool clear = sunrise == -1 && sunset == -1;
            result.sunrise_offset = Helpers.FormatAsValueRawAndUnit(
                clear ? "disabled" : sunrise.ToString(),
                sunrise,
                "min",
                _compact
            );
            result.sunset_offset = Helpers.FormatAsValueRawAndUnit(
                clear ? "disabled" : sunset.ToString(),
                sunset,
                "min",
                _compact
            );
            result.latitude = Helpers.FormatAsValueAndUnit(_parser.GetInt16() / 100, "°", _compact);
            result.longitude = Helpers.FormatAsValueAndUnit(_parser.GetInt16() / 100, "°", _compact);
            return result;
        }

        private dynamic DecodeStatusConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("status_config_packet", _compact);
            result.status_interval = Helpers.FormatAsValueAndUnit(_parser.GetUInt32(), "s", _compact);
            return result;
        }

        private dynamic DecodeProfileConfig()
        {
            dynamic result = new ExpandoObject();
            result.packet_type = Helpers.FormatAsValue("profile_config_packet", _compact);

            // result.

            return result;
        }


    }
}
