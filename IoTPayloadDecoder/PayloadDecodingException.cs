using System;

namespace IoTPayloadDecoder
{
    internal class PayloadDecodingException : Exception
    {
        public PayloadDecodingException()
        {
        }

        public PayloadDecodingException(string message) : base(message)
        {
        }

        public PayloadDecodingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
