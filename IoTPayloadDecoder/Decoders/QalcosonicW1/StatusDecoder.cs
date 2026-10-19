using System.Collections.Generic;

namespace IoTPayloadDecoder.Decoders.QalcosonicW1
{
    internal static class StatusDecoder
    {
        public static string DecodeStatusText(byte status)
        {
            var flags = DecodeStatusFlags(status);

            if (flags.Length == 1)
            {
                return flags[0];
            }

            return string.Join(" + ", flags);
        }

        public static string[] DecodeStatusFlags(byte status)
        {
            var statuses = new List<string>();

            if (status == 0x00)
            {
                return new[] { "No error" };
            }

            byte alarmStatus = (byte)(status & 0xF0);

            switch (alarmStatus)
            {
                case 0x10:
                    statuses.Add("Empty pipe");
                    break;

                case 0x30:
                    statuses.Add("Leakage");
                    break;

                case 0x70:
                    statuses.Add("Backflow");
                    break;

                case 0x90:
                    statuses.Add("Freeze");
                    break;

                case 0xB0:
                    statuses.Add("Burst");
                    break;
            }

            if ((status & 0x08) != 0)
            {
                statuses.Add("Permanent error");
            }

            if ((status & 0x04) != 0)
            {
                statuses.Add("Low battery");
            }

            if (statuses.Count == 0)
            {
                statuses.Add(string.Format(
                    "Unknown status 0x{0:X2}",
                    status));
            }

            return statuses.ToArray();
        }
    }
}