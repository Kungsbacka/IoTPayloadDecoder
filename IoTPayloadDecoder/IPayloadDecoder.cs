namespace IoTPayloadDecoder
{
    public interface IPayloadDecoder
    {
        dynamic Decode(string payloadString, bool compact);
    }
}
