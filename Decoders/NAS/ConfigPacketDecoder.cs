using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Text;

namespace IoTPayloadDecoder.Decoders.NAS
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
                    // decodeLdrConfig(dataView, result);
                    break;
                case 0x03:
                    // decodeDigConfig(dataView, result, err);
                    break;
                case 0x05:
                    result.packet_type =  Helpers.WrapAsValue("open_drain_out_config_packet", _compact);
                    //result.switching_steps = [];
                    //while (dataView.availableLen())
                    //{
                    //    result.switching_steps.push(decodeOdRelaySwStep(dataView));
                    //}
                    break;
                case 0x06:
                    // decodeCalendarConfig(dataView, result);
                    break;
                case 0x07:
                    // decodeStatusConfig(dataView, result);
                    break;
                case 0x08:
                    // decodeProfileConfig(dataView, result, err);
                    break;
                case 0x09:
                    // decodeTimeConfig(dataView, result);
                    break;
                case 0x0A:
                    // decodeLegacyDefaultsConfig(dataView, result);
                    break;
                case 0x0B:
                    // decodeUsageConfig(dataView, result);
                    break;
                case 0x0C:
                    // decodeHolidayConfig(dataView, result);
                    break;
                case 0x0D:
                    // decodeBootDelayConfig(dataView, result);
                    break;
                case 0x0E:
                    // decodeDefaultsConfig(dataView, result);
                    break;
                case 0x13:
                    // decodeLocationConfig(dataView, result);
                    break;
                case 0x15:
                    // decodeLedConfig(dataView, result);
                    break;
                case 0x16:
                    // decodeMeteringAlertConfig(dataView, result, err);
                    break;
                case 0x52:
                    // decodeMulticastConfig(dataView, result, err);
                    break;
                case 0xFF:
                    // decodeClearConfig(dataView, result, err);
                    break;
                default:
                    _errorList.Add("invalid_header");
                    break;
            }
            
            result.errors = _errorList.ToArray();
            return result;
        }
    }
}
