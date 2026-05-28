using IoTPayloadDecoder.Decoders.EMUProfessionalII;

namespace IoTPayloadDecoder.Tests.Decoders
{
	public class EMUProfessionalIITests
    {
		[Fact]
		public void DecodeDefaultUplink_ReturnsExpectedData()
		{
			string payload = "80848d65034e61bc0004e8f341000506120f000640e20100ff00a6";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, true);

			Assert.Equal(new DateTime(2023, 12, 28, 14, 21, 52, DateTimeKind.Utc), (DateTime)result.meterTimeUtc);
			Assert.Equal(12345678, (int)result.activeEnergyImportT1Wh);
			Assert.Equal(4322280, (int)result.activeEnergyImportT2Wh);
			Assert.Equal(987654, (int)result.activeEnergyExportT1Wh);
			Assert.Equal(123456, (int)result.activeEnergyExportT2Wh);
			Assert.Equal("", (string)result.errorCode);
			Assert.Equal(0, (int)result.warnings.Length);
		}

		[Fact]
		public void DecodeEnergyImport_ReturnsDecodedResultInWh()
		{
			// 034e61bc00 = Type Active Energy Import T1, 12345678 Wh
			// 04e8f34100 = Type Active Energy Import T2 = 4322280 Wh
			string payload = "80848d65034e61bc0004e8f3410005000000000600000000ff0090";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(12345678, (int)result.activeEnergyImportT1Wh.value);
			Assert.Equal("Wh", (string)result.activeEnergyImportT1Wh.unit);
			Assert.Equal(4322280, (int)result.activeEnergyImportT2Wh.value);
			Assert.Equal("Wh", (string)result.activeEnergyImportT2Wh.unit);
		}

		[Fact]
		public void DecodeEnergyExport_ReturnsDecodedResultInWh()
		{
			// 0506120f00 = Type Active Energy Export T1, 987654 Wh
			// 0640e20100 = Type Active Energy Export T2 = 123456 Wh
			string payload = "80848d65030000000004000000000506120f000640e20100ff0017";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(987654, (int)result.activeEnergyExportT1Wh.value);
			Assert.Equal("Wh", (string)result.activeEnergyExportT1Wh.unit);
			Assert.Equal(123456, (int)result.activeEnergyExportT2Wh.value);
			Assert.Equal("Wh", (string)result.activeEnergyExportT2Wh.unit);
		}

		[Fact]
		public void DecodeActivePower_ReturnsDecodedResultInW()
		{
			string payload = "80848d65034e61bc0004e8f341000506120f000640e201000bac0d0000100820000011781e00001234210000f022bd";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(3500, (int)result.activePowerL123.value);
			Assert.Equal("W", (string)result.activePowerL123.unit);
		}

		[Fact]
		public void DecodeCurrentPerPhase_ReturnsDecodedResultInMA()
		{
			string payload = "80848d65034e61bc0004e8f341000506120f000640e201000bac0d0000100820000011781e00001234210000f022bd";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(8200, (int)result.currentL1.value);
			Assert.Equal("mA", (string)result.currentL1.unit);
			Assert.Equal(7800, (int)result.currentL2.value);
			Assert.Equal("mA", (string)result.currentL2.unit);
			Assert.Equal(8500, (int)result.currentL3.value);
			Assert.Equal("mA", (string)result.currentL3.unit);
		}

		[Fact]
		public void DecodeDefaultUplink_ReturnsCorrectErrors()
		{
			// ff22 = Type Error code, error code 0x22
			string payload = "80848d65030000000004000000000506120f000640e20100ff22f9";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal("Current transformer ratio adjusted, Voltage interruption", (string)result.errorCode.value);
		}

		[Fact]
		public void DecodeExtendedFormat_ReturnsCorrectErrors()
		{
			// f022 = Type Error code, error code 0x22
			string payload = "80848d65034e61bc0004e8f341000506120f000640e201000bac0d0000100820000011781e00001234210000f048ac";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal("Impulse length adjusted, Time not valid or not synchronized", (string)result.errorCode.value);
		}
	}
}
