using System;
using System.Collections.Generic;
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

        internal static dynamic FormatAsValue<T>(T value, bool compact)
        {
            if (compact)
            {
                return value;
            }
            dynamic result = new ExpandoObject();
            result.value = value;
            return result;
        }

        internal static dynamic FormatAsValueAndRaw<T1, T2>(T1 value, T2 raw, bool compact)
        {
            if (compact)
            {
                return value;
            }
            dynamic result = new ExpandoObject();
            result.value = value;
            result.raw = raw;
            return result;
        }

        internal static dynamic FormatAsValueAndUnit<T>(T value, string unit, bool compact)
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

        internal static dynamic FormatAsValueUnitAndMinMax<T>(T value, string unit, int min, int max, bool compact)
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

        internal static dynamic FormatAsValueRawAndUnit<T1, T2>(T1 value, T2 raw, string unit, bool compact)
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

        internal static dynamic DecodeProfile(PayloadParser parser, List<string> errorList, bool compact)
        {
            dynamic profile = new ExpandoObject();
            byte id = parser.GetUInt8();
            byte version = parser.GetUInt8();
            byte address = parser.GetUInt8();
            profile.profile_id = id == 255
                ? Helpers.FormatAsValueAndRaw("no_profile", id, compact)
                : Helpers.FormatAsValue(id, compact);
            profile.profile_version = version > 240
                ? Helpers.FormatAsValueAndRaw("n/a", version, compact)
                : Helpers.FormatAsValue(version, compact);
            profile.profile_override = Helpers.FormatAsValueAndRaw(GetProfileOverrideReason(version), version, compact);
            string daliAddress = Helpers.ConvertToDaliAddress(address);
            if (Helpers.IsInvalidDaliAddress(daliAddress))
            {
                errorList.Add("Invalid DALI address");
            }
            profile.dali_address_short = Helpers.FormatAsValueAndRaw(daliAddress, address, compact);
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
            profile.days_active = Helpers.FormatAsValueAndRaw(days, active_days, compact);
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
