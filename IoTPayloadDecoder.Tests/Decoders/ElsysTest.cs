using IoTPayloadDecoder.Tests.TestHelpers;
using Xunit.Abstractions;

namespace IoTPayloadDecoder.Tests.Decoders
{
    public class ElsysTest
    {
        private readonly ITestOutputHelper _output;

        private readonly IPayloadDecoder _genericDecoder =
            DecoderFactory.Create(DeviceModel.Elsys, 6);

        public ElsysTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Decode_Port2MessageFormat1_ShouldDecodeExpectedValues()
        {
            var payload = "3e610701080509010a000b050d000c051000130000000014000002581500000001160000000117000000011a000000001b00";

            dynamic result = _genericDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);


        }
    }
}
