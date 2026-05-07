using IoTPayloadDecoder.Decoders.CMi4170;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoTPayloadDecoder.Tests.Decoders
{
    public class CMi4170Tests
    {
		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedData()
		{
			
			string payload = "240406DC0500000413A05B0000022BC409023BC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, true);

			Assert.Equal(0x24, result.messageFormat); // 24 = 0x24
			Assert.Equal(1500.0, result.energy); // DC050000 = 1500
			Assert.Equal(23.456, result.volume); // A05B0000 = 23456
			Assert.Equal(2500.0, result.power); // C409 = 2500
			Assert.Equal(0.450, result.flow); // C201 = 450
			Assert.Equal(65.0, result.forwardTemperature); // 4100 = 65
			Assert.Equal(65.0, result.returnTemperature); // 4100 = 65
			Assert.Equal("21957374", result.meterId); // 74739521 = 21957374
			Assert.Equal(0x00, result.errorFlags); //00 = 0x00
			Assert.Equal(0, result.warnings.Length);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedEnergy_WhenFormatIsWh()
		{
			// 0403DC050000 = 1500 Wh
			string payload = "240403DC0500000413A05B0000022BC409023BC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(1.5, result.energy.value); // 1500 Wh = 1.5 kWh
			Assert.Equal("kWh", result.energy.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedEnergy_WhenFormatIs10MJ()
		{
			// 04OFDC050000 = 10 * 1500 MJ
			string payload = "24040FDC0500000413A05B0000022BC409023BC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(10 * 1500 / 3.6, result.energy.value); // 10 * 1500 MJ / 3.6 = 4166.67 kWh
			Assert.Equal("kWh", result.energy.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedEnergy_WhenFormatIsMCal()
		{
			// 04FB0DDC050000 = 1500 MCal
			string payload = "2404FB0DDC0500000413A05B0000022BC409023BC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(1500 * 1.1622, result.energy.value); // 1500 MCal * 1.1622 = 1743.33 kWh
			Assert.Equal("kWh", result.energy.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedVolume_WhenFormatIs10m3()
		{
			// 0417A05B0000 = 10 * 23456 m3
			string payload = "2404FB0DDC0500000417A05B0000022BC409023BC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(234560, result.volume.value); // 10 * 23456 m3 = 234560 m3
			Assert.Equal("m³", result.volume.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedVolume_WhenFormatIs00001m3()
		{
			// 0412A05B0000 = 0.0001 * 23456 m3
			string payload = "2404FB0DDC0500000412A05B0000022BC409023BC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(2.3456, result.volume.value); // 0.0001 * 23456 m3 = 2.3456 m3
			Assert.Equal("m³", result.volume.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedPower_WhenFormatIs10W()
		{
			// 022CC409 = 10 * 2500 W
			string payload = "2404FB0DDC0500000412A05B0000022CC409023BC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(25000.0, result.power.value); // 10 * 2500 W = 25000 W
			Assert.Equal("W", result.power.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedPower_WhenFormatIs10kW()
		{
			// 022FC409 = 10 * 2500 kW
			string payload = "2404FB0DDC0500000412A05B0000022FC409023BC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(25000000.0, result.power.value); // 10 * 2500 kW * 1000 = 25000000 W
			Assert.Equal("W", result.power.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedFlow_WhenFormatIs001m3h()
		{
			// 023CC201 = 0.01 * 450 m³/h
			string payload = "2404FB0DDC0500000412A05B0000022FC409023CC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(4.5, result.flow.value); // 0.01 * 450 m³/h = 4.5 m³/h
			Assert.Equal("m³/h", result.flow.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedForwardTemp_WhenFormatIsC()
		{
			// 025B4100 = 65 °C
			string payload = "2404FB0DDC0500000412A05B0000022FC409023CC201025B4100025F41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(65, result.forwardTemperature.value); // 1 * 65 °C = 65 °C
			Assert.Equal("°C", result.forwardTemperature.unit);
		}


		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedReturnTemp_WhenFormatIs01C()
		{
			// 025E4100 = 0.1 * 65 °C
			string payload = "2404FB0DDC0500000412A05B0000022FC409023CC201025B4100025E41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(6.5, result.returnTemperature.value); // 0.1 * 65 °C = 6.5 °C
			Assert.Equal("°C", result.returnTemperature.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedMeterId()
		{
			// 0C7874739521 = Meter ID 21957374 (BCD-kodat)
			string payload = "2404FB0DDC0500000412A05B0000022FC409023CC201025B4100025E41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal("21957374", result.meterId.value);
			Assert.Equal("count", result.meterId.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedErrorFlags()
		{
			// 01FD1700 = No error flags
			string payload = "2404FB0DDC0500000412A05B0000022FC409023CC201025B4100025E41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(0x00, result.errorFlags.value);
			Assert.Equal("count", result.errorFlags.unit);
		}

		[Fact]
		public void DecodeStandardFormat_ReturnsExpectedWarnings_WhenEnergyIsInFaultyFormat()
		{
			// 34FB0D = Faulty energy format
			string payload = "2434FB0DDC0500000412A05B0000022FC409023CC201025B4100025E41000C787473952101FD1700";
			var decoder = new DataDecoder();
			var result = decoder.Decode(payload, false);

			Assert.Equal(1, result.warnings.Length);
			Assert.Equal("Energy value is in error state", result.warnings[0]);
		}
	}
}
