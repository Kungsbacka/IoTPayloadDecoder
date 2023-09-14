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
                        return new Decoders.NAS11.StatusPacketDecoder();
                    case 26:
                        return new Decoders.NAS11.UsagePacketDecoder();
                    case 49:
                        throw new NotImplementedException("Decoder for port 49 is not yet implemented");
                    case 50:
                        return new Decoders.NAS11.ConfigPacketDecoder();
                    case 51:
                        throw new NotImplementedException("Decoder for port 51 is not yet implemented");
                    case 60:
                        throw new NotImplementedException("Decoder for port 60 is not yet implemented");
                    case 61:
                        throw new NotImplementedException("Decoder for port 61 is not yet implemented");
                    case 99:
                        return new Decoders.NAS11.BootPacketDecoder();
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
