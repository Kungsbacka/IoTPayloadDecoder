using System;
using System.ComponentModel;
using System.Reflection;

namespace IoTPayloadDecoder
{
    public enum Unit
    {
        [Description("B")] Byte,
        [Description("count")] Count,
        [Description("dB")] Decibel,
        [Description("dBm")] DecibelMilliwat,
        [Description("°")] Degree,
        [Description("°C")] DegreeCelsius,
        [Description("hPa")] HectoPascal,
        [Description("hh:mm")] HourAndMinute,
        [Description("lx")] Lux,
        [Description("uV")] Microvolt,
        [Description("mm")] Millimeter,
        [Description("mV")] Millivolt,
        [Description("min")] Minute,
        [Description("ppb")] PartsPerBillion,
        [Description("ppm")] PartsPerMillion,
        [Description("%")] Percent,
        [Description("relative count")] RelativeCount,
        [Description("s")] Second,
        [Description("??")] Unknown,
        [Description("V")] Volt,
        [Description("W")] Watt,
        [Description("Wh")] WattHour,

        [Description("1/63G")] OneSixtythirdG,
        [Description("bool")] Boolean
    }

    public static class UnitExtension
    {
        public static string ToUnitString(this Unit unit)
        {
            Type type = unit.GetType();
            string name = Enum.GetName(type, unit);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
					if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
					{
						return attr.Description;
					}
				}
            }
            return null;
        }
    }
}
