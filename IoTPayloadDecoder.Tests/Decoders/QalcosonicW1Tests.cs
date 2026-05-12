using IoTPayloadDecoder.Tests.TestHelpers;
using Xunit.Abstractions;

namespace IoTPayloadDecoder.Tests.Decoders
{
    public class QalcosonicW1Tests
    {
        private readonly ITestOutputHelper _output;

        private readonly IPayloadDecoder _port100RegularDataDecoder =
            DecoderFactory.Create(DeviceModel.QalcosonicW1, 100);

        public QalcosonicW1Tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Decode_BasicPayload_ShouldDecodeExpectedValues()
        {
            var payload = "83d6366210964b251a964b251a964b251a964b251a964b251a964b251a964b251a100e0000";

            dynamic result = _port100RegularDataDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);

            DateTime meterTimeUtc = result.meterTimeUtc.value;
            byte status = result.status.value;
            uint currentVolume = result.currentVolume.value;
            uint pastVolume1 = result.pastVolume1.value;
            uint pastVolume6 = result.pastVolume6.value;
            uint periodBetweenValues = result.periodBetweenValues.value;

            Assert.Equal(new DateTime(2022, 3, 20, 7, 23, 47, DateTimeKind.Utc), meterTimeUtc);
            Assert.Equal((byte)0x10, status);
            Assert.Equal(438651798u, currentVolume);
            Assert.Equal(438651798u, pastVolume1);
            Assert.Equal(438651798u, pastVolume6);
            Assert.Equal(3600u, periodBetweenValues);
            Assert.Empty(result.warnings);
        }
    }
}
