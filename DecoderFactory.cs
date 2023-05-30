using System;

namespace IoTPayloadDecoder
{
    public static class DecoderFactory
    {
        public static IPayloadDecoder Create(DeviceModel model, int port)
        {
            if (model == DeviceModel.Nas10)
            {
                switch (port)
                {
                    case 24:
                        return new Decoders.NAS10.StatusPacketDecoder();
                    case 25:
                        return new Decoders.NAS10.UsagePacketDecoder();
                    case 99:
                        return new Decoders.NAS10.BootPacketDecoder();
                    default:
                        throw new ArgumentException("No decoder found for port");
                }
            }
            if (model == DeviceModel.Nas11)
            {
                switch (port)
                {
                    case 23:
                        return new Decoders.NAS10.StatusPacketDecoder();
                    case 26:
                        return new Decoders.NAS10.UsagePacketDecoder();
                    case 99:
                        return new Decoders.NAS10.BootPacketDecoder();
                    default:
                        throw new ArgumentException("No decoder found for port");
                }
            }
            if (model == DeviceModel.Elsys)
            {
                return new Decoders.Elsys.GenericDecoder();
            }
            throw new ArgumentException("No decoder found for model");
        }
    }
}
