using IoTPayloadDecoder.Tests.TestHelpers;
using Xunit.Abstractions;

namespace IoTPayloadDecoder.Tests.Decoders
{
    public class LHi110Tests
    {
        private readonly ITestOutputHelper _output;

        private readonly IPayloadDecoder _port1ProtocolDataDecoder =
            DecoderFactory.Create(DeviceModel.LHi110, 1);

        private readonly IPayloadDecoder _port2PeriodicDataDecoder =
            DecoderFactory.Create(DeviceModel.LHi110, 2);

        public LHi110Tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Decode_Port2MessageFormat1_ShouldDecodeExpectedValues()
        {
            var payload = "016774858000000186A00064005000C800A0012C00F0398E739A00A0501E";

            dynamic result = _port2PeriodicDataDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);

            Assert.Equal(1, result.messageFormat.value);
            Assert.Equal((ulong)100000, result.activeImportReading.value);
            Assert.Empty(result.warnings);
        }

        [Fact]
        public void Decode_Port1CpuVoltage_ShouldDecodeExpectedValue()
        {
            var payload = "01060BB8";

            dynamic result = _port1ProtocolDataDecoder.Decode(payload, compact: false);

            JsonTestOutput.PrintResult(_output, result);

            Assert.Equal(3000, result.cpuVoltage.value);
            Assert.Empty(result.warnings);

        }
    }
}
