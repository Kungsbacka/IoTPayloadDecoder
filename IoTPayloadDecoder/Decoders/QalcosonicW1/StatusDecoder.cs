using System.Collections.Generic;

internal static class StatusDecoder
{
    public static string[] Decode(byte status)
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
                statuses.Add("Negative flow / Backflow");
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

        return statuses.ToArray();
    }
}