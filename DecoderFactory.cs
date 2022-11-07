using System;

namespace IoTPayloadDecoder
{
    public static class DecoderFactory
    {
        public static IPayloadDecoder Create(DeviceModel model, int port)
        {
            if (model == DeviceModel.Nas)
            {
                switch (port)
                {
                    case 24:
                        return new Decoders.NAS.StatusPacketDecoder();
                    case 25:
                        return new Decoders.NAS.UsagePacketDecoder();
                    case 99:
                        return new Decoders.NAS.BootPacketDecoder();
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
