using System;

namespace IoTPayloadDecoder
{
    internal class PayloadParsingException : Exception
    {
        public PayloadParsingException()
        {
        }

        public PayloadParsingException(string message) : base(message)
        {
        }

        public PayloadParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
