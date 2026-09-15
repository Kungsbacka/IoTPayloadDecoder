using IoTPayloadDecoder.Tests.TestHelpers;
using Xunit.Abstractions;

namespace IoTPayloadDecoder.Tests.Decoders
{
    public class QalcosonicW1Tests
    {
        private readonly ITestOutputHelper _output;

        private readonly IPayloadDecoder _port100RegularDataDecoder =
            DecoderFactory.Create(DeviceModel.QalcosonicW1, 100);

        private readonly IPayloadDecoder _port101ConfigParamsDecoder =
            DecoderFactory.Create(DeviceModel.QalcosonicW1, 101);

        private readonly IPayloadDecoder _port103DeviceAlarmDecoder =
            DecoderFactory.Create(DeviceModel.QalcosonicW1, 103);

        public QalcosonicW1Tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DecodeBasicPayload()
        {
            var payload = "012290456ab0886500000000000000001da1a6c05120000c00000000000000000000000000a9640000000000000000000000";

            dynamic result = _port100RegularDataDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);
        }

        [Fact]
        public void DecodeAlarmPayload()
        {
            var payload = "774f3b6a00";

            dynamic result = _port103DeviceAlarmDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);
        }
        [Fact]
        public void DecodeAlarmPayload2()
        {
            var payload = "cb23f9b6edc29e0bd933e46dfce8fe0d";

            dynamic result = _port103DeviceAlarmDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);
        }
        [Fact]
        public void DecodeAlarmPayload3()
        {
            var payload = "316117b52365e771bd1e17603515726c";

            dynamic result = _port103DeviceAlarmDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);
        }
        [Fact]
        public void DecodeAlarmPayload4()
        {
            var payload = "5424cbf11952fa9d086f180bd1439bd1";

            dynamic result = _port103DeviceAlarmDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);
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
