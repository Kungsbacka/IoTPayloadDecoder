using System.Dynamic;

namespace IoTPayloadDecoder
{
    internal static class ElsysDecoder
    {
        const byte TYPE_TEMP = 0x01; // Temp 2 bytes -3276.8°C -->3276.7°C
        const byte TYPE_RH = 0x02; // Humidity 1 byte  0-100%
        const byte TYPE_ACC = 0x03; // Acceleration 3 bytes X,Y,Z -128 --> 127 +/-63=1G
        const byte TYPE_LIGHT = 0x04; // Light 2 bytes 0-->65535 Lux
        const byte TYPE_MOTION = 0x05; // No of motion 1 byte  0-255
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
        const byte TYPE_PULSE2_ABS = 0x17; //4 bytes no 0->0xFFFFFFFF
        const byte TYPE_ANALOG2 = 0x18; // 2 bytes voltage in mV
        const byte TYPE_EXT_TEMP2 = 0x19; // 2 bytes -3276.5C-->3276.5C
        const byte TYPE_EXT_DIGITAL2 = 0x1A; // 1 bytes value 1 or 0
        const byte TYPE_EXT_ANALOG_UV = 0x1B; // 4 bytes signed int (uV)
        const byte TYPE_TVOC = 0x1C; // 2 bytes (ppb)
        const byte TYPE_DEBUG = 0x3D; // 4 bytes debug

        public static dynamic Decode(string payload)
        {
            dynamic result = new ExpandoObject();
            PayloadParser parser = new(payload);
            List<double> externalTemperature2 = new();
            while (parser.RemainingBits > 0)
            {
                switch (parser.GetUInt8())
                {
                    case TYPE_TEMP:
                        result.temperature = parser.GetInt16BE() / 10d;
                        break;
                    case TYPE_RH:
                        result.humidity = parser.GetInt8();
                        break;
                    case TYPE_ACC:
                        result.x = parser.GetInt8();
                        result.y = parser.GetInt8();
                        result.z = parser.GetInt8();
                        break;
                    case TYPE_LIGHT:
                        result.light = parser.GetUInt16BE();
                        break;
                    case TYPE_MOTION:
                        result.motion = parser.GetUInt8();
                        break;
                    case TYPE_CO2:
                        result.co2 = parser.GetUInt16BE();
                        break;
                    case TYPE_VDD:
                        result.vdd = parser.GetUInt16BE();
                        break;
                    case TYPE_ANALOG1:
                        result.analog1 = parser.GetUInt16BE();
                        break;
                    case TYPE_GPS: // Not big endian! (See reference implementation)
                        result.lat = parser.GetInt24() / 10000d;
                        result["long"] = parser.GetInt24() / 10000d;
                        break;
                    case TYPE_PULSE1:
                        result.pulse1 = parser.GetUInt16BE();
                        break;
                    case TYPE_PULSE1_ABS:
                        result.pulseAbs = parser.GetUInt32BE();
                        break;
                    case TYPE_EXT_TEMP1:
                        result.externalTemperature = parser.GetInt16BE() / 10d;
                        break;
                    case TYPE_EXT_DIGITAL:
                        result.digital = parser.GetUInt8();
                        break;
                    case TYPE_EXT_DISTANCE:
                        result.distance = parser.GetUInt16BE();
                        break;
                    case TYPE_ACC_MOTION:
                        result.accMotion = parser.GetUInt8();
                        break;
                    case TYPE_IR_TEMP:
                        result.irInternalTemperature = parser.GetInt16BE() / 10d;
                        result.irExternalTemperature = parser.GetInt16BE() / 10d;
                        break;
                    case TYPE_OCCUPANCY:
                        result.occupancy = parser.GetUInt8();
                        break;
                    case TYPE_WATERLEAK:
                        result.waterleak = parser.GetUInt8();
                        break;
                    case TYPE_GRIDEYE:
                        byte r = parser.GetUInt8();
                        double[] grideye = new double[64];
                        for (int i = 0; i < 64; i++)
                        {
                            grideye[i] = r + parser.GetUInt8() / 10d;
                        }
                        result.grideye = grideye;
                        break;
                    case TYPE_PRESSURE:
                        result.pressure = parser.GetUInt32BE() / 1000d;
                        break;
                    case TYPE_SOUND:
                        result.soundPeak = parser.GetUInt8();
                        result.soundAvg = parser.GetUInt8();
                        break;
                    case TYPE_PULSE2:
                        result.pulse2 = parser.GetUInt16BE();
                        break;
                    case TYPE_PULSE2_ABS:
                        result.pulseAbs2 = parser.GetUInt32BE();
                        break;
                    case TYPE_ANALOG2:
                        result.analog2 = parser.GetUInt16BE();
                        break;
                    case TYPE_EXT_TEMP2:
                        externalTemperature2.Add(parser.GetInt16BE() / 10d);
                        break;
                    case TYPE_EXT_DIGITAL2:
                        result.digital2 = parser.GetUInt8();
                        break;
                    case TYPE_EXT_ANALOG_UV:
                        result.analogUV = parser.GetUInt32BE();
                        break;
                    case TYPE_TVOC:
                        result.tvoc = parser.GetUInt16BE();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid data in payload");
                }
            }
            if (externalTemperature2.Count == 1)
            {
                result.externalTemperature2 = externalTemperature2[0];
            }
            if (externalTemperature2.Count > 1)
            {
                result.externalTemperature2 = externalTemperature2;
            }
            return result;
        }
    }
}
