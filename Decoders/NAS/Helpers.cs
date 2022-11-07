using System;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders.NAS
{
    internal static class Helpers
    {
        internal static bool IsValidPayloadString(string payloadString)
        {
            if (string.IsNullOrEmpty(payloadString))
            {
                return false;
            }
            if (payloadString.Length % 2 != 0)
            {
                return false;
            }
            foreach (char c in payloadString)
            {
                if (!IsHex(c))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsHex(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        internal static dynamic WrapAsValue<T>(T value, bool compact)
        {
            if (compact)
            {
                return (T)value;
            }
            dynamic result = new ExpandoObject();
            result.value = value;
            return result;
        }

        internal static dynamic WrapAsValueAndRaw<T1, T2>(T1 value, T2 raw, bool compact)
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

        internal static dynamic WrapAsValueAndUnit<T>(T value, string unit, bool compact)
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

        internal static dynamic WrapAsValueUnitAndMinMax<T>(T value, string unit, int min, int max, bool compact)
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

        internal static dynamic WrapAsValueRawAndUnit<T1, T2>(T1 value, T2 raw, string unit, bool compact)
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

        internal static string ConvertToDaliAddress(byte address, string ff_str = null)
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
                return "invalid";
            }
            if ((address & 0x01) > 0)
            {
                return "invalid";
            }
            if ((address & 0x80) > 0)
            {
                return string.Concat("group ", ((address >> 1) & 0xf));
            }
            return string.Concat("single " + ((address >> 1) & 0x3f));
        }

        internal static bool IsInvalidDaliAddress(string address)
        {
            return string.Equals("invalid", address, StringComparison.OrdinalIgnoreCase);
        }

    }
}
