using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders.Elsys
{
    internal class GenericDecoder : IPayloadDecoder
    {
        const byte TYPE_TEMP = 0x01; // Temp 2 bytes -3276.8°C -->3276.7°C
        const byte TYPE_RH = 0x02; // Humidity 1 byte  0-100%
        const byte TYPE_ACC = 0x03; // Acceleration 3 bytes X,Y,Z -128 --> 127 +/-63=1G
        const byte TYPE_LIGHT = 0x04; // Light 2 bytes 0-->65535 Lux
        const byte TYPE_MOTION = 0x05; // No of motion 1 byte 0-255
        const byte TYPE_CO2 = 0x06; // Co2 2 bytes 0-65535 ppm
        const byte TYPE_VDD = 0x07; // VDD 2 byte 0-65535mV
        const byte TYPE_ANALOG1 = 0x08; // VDD 2 byte 0-65535mV
        const byte TYPE_GPS = 0x09; // 3 bytes lat 3bytes long binary
        const byte TYPE_PULSE1 = 0x0A; // 2 bytes relative pulse count
        const byte TYPE_PULSE1_ABS = 0x0B; // 4 bytes no 0->0xFFFFFFFF
        const byte TYPE_EXT_TEMP1 = 0x0C; // 2 bytes -3276.5C-->3276.5C
        const byte TYPE_EXT_DIGITAL = 0x0D; // 1 bytes value 1 or 0
        const byte TYPE_EXT_DISTANCE = 0x0E; // 2 bytes distance in mm
        const byte TYPE_ACC_MOTION = 0x0F; // 1 byte number of vibration/motion
        const byte TYPE_IR_TEMP = 0x10; // 2 bytes internal temp 2bytes external temp -3276.5C-->3276.5C
        const byte TYPE_OCCUPANCY = 0x11; // 1 byte data
        const byte TYPE_WATERLEAK = 0x12; // 1 byte data 0-255
        const byte TYPE_GRIDEYE = 0x13; // 65 byte temperature data 1byte ref+64byte external temp
        const byte TYPE_PRESSURE = 0x14; // 4 byte pressure data (hPa)
        const byte TYPE_SOUND = 0x15; // 2 byte sound data (peak/avg)
        const byte TYPE_PULSE2 = 0x16; // 2 bytes 0-->0xFFFF
        const byte TYPE_PULSE2_ABS = 0x17; // 4 bytes no 0->0xFFFFFFFF
        const byte TYPE_ANALOG2 = 0x18; // 2 bytes voltage in mV
        const byte TYPE_EXT_TEMP2 = 0x19; // 2 bytes -3276.5C-->3276.5C
        const byte TYPE_EXT_DIGITAL2 = 0x1A; // 1 bytes value 1 or 0
        const byte TYPE_EXT_ANALOG_UV = 0x1B; // 4 bytes signed int (uV)
        const byte TYPE_TVOC = 0x1C; // 2 bytes (ppb)
        const byte TYPE_DEBUG = 0x3D; // 4 bytes debug

        bool _compact;
        dynamic _result;

        public dynamic Decode(string payload, bool compact)
        {
            _result = new ExpandoObject();
            _compact = compact;
            var parser = new PayloadParser(payload);
            var externalTemperature2 = new List<double>();
            while (parser.RemainingBits > 15)
            {
                uint type = parser.GetUInt8();
                switch (type)
                {
                    case TYPE_TEMP:
                        AddResult("temperature", parser.GetInt16BE() / 10d, Unit.DegreeCelsius);
                        break;
                    case TYPE_RH:
                        AddResult("humidity", parser.GetInt8(), Unit.Percent);
                        break;
                    case TYPE_ACC:
                        AddResult("x", parser.GetInt8(), Unit.OneSixtythirdG);
                        AddResult("y", parser.GetInt8(), Unit.OneSixtythirdG);
                        AddResult("z", parser.GetInt8(), Unit.OneSixtythirdG);
                        break;
                    case TYPE_LIGHT:
                        AddResult("light", parser.GetUInt16BE(), Unit.Lux);
                        break;
                    case TYPE_MOTION:
                        AddResult("motion", parser.GetUInt8(), Unit.Unknown);
                        break;
                    case TYPE_CO2:
                        AddResult("co2", parser.GetUInt16BE(), Unit.PartsPerMillion);
                        break;
                    case TYPE_VDD:
                        AddResult("vdd", parser.GetUInt16BE(), Unit.Millivolt);
                        break;
                    case TYPE_ANALOG1:
                        AddResult("analog1", parser.GetUInt16BE(), Unit.Millivolt);
                        break;
                    case TYPE_GPS: // Everything is big endian except GPS (See reference implementation)
                        AddResult("lat", parser.GetInt24() / 10000d, Unit.Unknown);
                        AddResult("long", parser.GetInt24() / 10000d, Unit.Unknown);
                        break;
                    case TYPE_PULSE1:
                        AddResult("pulse1", parser.GetUInt16BE(), Unit.RelativeCount);
                        break;
                    case TYPE_PULSE1_ABS:
                        // The comment in the reference implementation say that this is a unsigned int,
                        // but the code returns an signed int. This decoder follows the comment and returns
                        // an unsigned int.
                        AddResult("pulseAbs", parser.GetUInt32BE(), Unit.Count);
                        break;
                    case TYPE_EXT_TEMP1:
                        AddResult("externalTemperature", parser.GetInt16BE() / 10d, Unit.DegreeCelsius);
                        break;
                    case TYPE_EXT_DIGITAL:
                        AddResult("digital", parser.GetBit(), Unit.Boolean);
                        // result.digital = parser.GetUInt8();
                        break;
                    case TYPE_EXT_DISTANCE:
                        AddResult("distance", parser.GetUInt16BE(), Unit.Millimeter);
                        break;
                    case TYPE_ACC_MOTION:
                        AddResult("accMotion", parser.GetUInt8(), Unit.Unknown);
                        break;
                    case TYPE_IR_TEMP:
                        AddResult("irInternalTemperature", parser.GetInt16BE() / 10d, Unit.DegreeCelsius);
                        AddResult("irExternalTemperature", parser.GetInt16BE() / 10d, Unit.DegreeCelsius);
                        break;
                    case TYPE_OCCUPANCY:
                        AddResult("occupancy", parser.GetUInt8(), Unit.Unknown);
                        break;
                    case TYPE_WATERLEAK:
                        AddResult("waterleak", parser.GetUInt8(), Unit.Unknown);
                        break;
                    case TYPE_GRIDEYE:
                        byte r = parser.GetUInt8();
                        double[] grideye = new double[64];
                        for (int i = 0; i < 64; i++)
                        {
                            grideye[i] = r + parser.GetUInt8() / 10d;
                        }
                        AddResult("grideye", grideye, Unit.DegreeCelsius);
                        break;
                    case TYPE_PRESSURE:
                        // Reference implementation will return a signed value. I don't know if a
                        // negative pressure is possible, but sometimes the sensors will return
                        // 0xffffffff and it's a little clearer that something is wrong if we
                        // return -0.001 instead of 4294967.295.
                        AddResult("pressure", parser.GetInt32BE() / 1000d, Unit.HectoPascal);
                        break;
                    case TYPE_SOUND:
                        AddResult("soundPeak", parser.GetUInt8(), Unit.Unknown);
                        AddResult("soundAvg", parser.GetUInt8(), Unit.Unknown);
                        break;
                    case TYPE_PULSE2:
                        AddResult("pulse2", parser.GetUInt16BE(), Unit.RelativeCount);
                        break;
                    case TYPE_PULSE2_ABS:
                        // The comment in the reference implementation say that this is a unsigned int,
                        // but the code returns an signed int. This decoder follows the comment and returns
                        // an unsigned int.
                        AddResult("pulseAbs2", parser.GetUInt16BE(), Unit.Count);
                        break;
                    case TYPE_ANALOG2:
                        AddResult("analog2", parser.GetUInt16BE(), Unit.Millivolt);
                        break;
                    case TYPE_EXT_TEMP2:
                        externalTemperature2.Add(parser.GetInt16BE() / 10d);
                        break;
                    case TYPE_EXT_DIGITAL2:
                        AddResult("digital2", parser.GetBit(), Unit.Boolean);
                        break;
                    case TYPE_EXT_ANALOG_UV:
                        AddResult("analogUV", parser.GetUInt32BE(), Unit.Microvolt);
                        break;
                    case TYPE_TVOC:
                        AddResult("tvoc", parser.GetUInt16BE(), Unit.PartsPerBillion);
                        break;
                    case TYPE_DEBUG:
                        AddResult("debug", parser.GetUInt32BE(), Unit.Unknown);
                        break;
                    default:
                        throw new PayloadDecodingException($"Unknown type 0x{type:X2}");
                }
            }
            if (externalTemperature2.Count == 1)
            {
                AddResult("externalTemperature2", externalTemperature2[0], Unit.DegreeCelsius);
            }
            if (externalTemperature2.Count > 1)
            {
                AddResult("externalTemperature2", externalTemperature2, Unit.DegreeCelsius);
            }
            return _result;
        }


        private void AddResult<T>(string name, T value, Unit unit)
        {
            if (_compact)
            {
                ((IDictionary<string,object>)_result).Add(name, string.Concat(value, " " , unit.ToUnitString()));
            }
            else
            {
                dynamic tmp = new ExpandoObject();
                tmp.value = value;
                tmp.unit = unit.ToUnitString();
                ((IDictionary<string, object>)_result).Add(name, tmp);
            }
        }
    }
}

