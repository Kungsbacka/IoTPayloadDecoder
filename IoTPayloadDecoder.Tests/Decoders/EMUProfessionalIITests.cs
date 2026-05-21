using IoTPayloadDecoder.Decoders.EMUProfessionalII;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoTPayloadDecoder.Tests.Decoders
{
	public class EMUProfessionalIITests
    {
		[Fact]
		public void DecodeDefaultUplink_ReturnsExpectedData()
		{
			string payload = "80848d65034e61bc0004e8f3410005000000000600000000ff0090";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, true);

			Assert.Equal(new DateTime(2023, 12, 28, 14, 21, 52, DateTimeKind.Utc), result.meterTimeUtc);
			Assert.Equal(12345.678, result.activeEnergyImportT1);
			Assert.Equal(4322.280, result.activeEnergyImportT2);
			Assert.Equal(0, result.activeEnergyExportT1); 
			Assert.Equal(0, result.activeEnergyExportT2);
			Assert.Equal(0, result.errorCode);
			Assert.Equal(0, result.warnings.Length);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsEnergyImportInKWh()
		{
			// 034e61bc00 = Type Active Energy Import T1, 12345678 Wh
			// 04e8f34100 = Type Active Energy Import T2 = 4322280 Wh
			string payload = "80848d65034e61bc0004e8f3410005000000000600000000ff0090";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(12345.678, result.activeEnergyImportT1.value);
			Assert.Equal("kWh", result.activeEnergyImportT1.unit);
			Assert.Equal(4322.280, result.activeEnergyImportT2.value);
			Assert.Equal("kWh", result.activeEnergyImportT2.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsEnergyExportInKWh()
		{
			// 0506120f00 = Type Active Energy Export T1, 12345678 Wh
			// 0640e20100 = Type Active Energy Export T2 = 4322280 Wh
			string payload = "80848d65030000000004000000000506120f000640e20100ff0017";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(987.654, result.activeEnergyExportT1.value);
			Assert.Equal("kWh", result.activeEnergyExportT1.unit);
			Assert.Equal(123.456, result.activeEnergyExportT2.value);
			Assert.Equal("kWh", result.activeEnergyExportT2.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsCorrectErrorCode()
		{
			// ff22 = Type Error code, error code 0x22
			string payload = "80848d65030000000004000000000506120f000640e20100ff22f9";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(0x22, result.errorCode.value);
			Assert.Equal("errCTRatioAdjusted, errVoltageInterruption", result.errorCodeDescription.value);
		}
	}
}
