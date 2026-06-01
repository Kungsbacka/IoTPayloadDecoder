using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders.CMi4170
{
	public class DataDecoder : IPayloadDecoder
	{
		private List<byte> TYPE_ENERGY = new List<byte> { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x0E, 0x0F, 0xFB };
		private List<byte> TYPE_VOLUME = new List<byte> { 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 };
		private List<byte> TYPE_POWER = new List<byte> { 0x2B, 0x2C, 0x2D, 0x2E, 0x2F };
		private List<byte> TYPE_FLOW = new List<byte> { 0x3B, 0x3C, 0x3D, 0x3E, 0x3F };
		private List<byte> TYPE_FWTEMP = new List<byte> { 0x58, 0x59, 0x5A, 0x5B };
		private List<byte> TYPE_RTTEMP = new List<byte> { 0x5C, 0x5D, 0x5E, 0x5F };

        private DecodingResult _decodingResult;
        private PayloadParser _parser;

		public dynamic Decode(string payloadString, bool compact)
		{
			if (string.IsNullOrWhiteSpace(payloadString))
			{
				throw new ArgumentException("Payload string cannot be empty", nameof(payloadString));
			}

			_parser = new PayloadParser(payloadString);
            _decodingResult = new DecodingResult(compact);

			DecodeData();

			return _decodingResult.FinishResult();
		}

		private void DecodeData()
		{
			byte messageFormat = _parser.GetUInt8();
			_decodingResult.AddResult("messageFormat", messageFormat);

			switch (messageFormat)
			{
				case 0x24:
					DecodeMessageFormatStandard();
					break;

				default:
					_decodingResult.AddWarning($"Unsupported message format: 0x{messageFormat:X2}");
					break;
			}
		}

		private void DecodeMessageFormatStandard()
		{
			while (_parser.RemainingBits > 0)
			{
				byte dif = _parser.GetUInt8(); // dataInfoByte
				byte vif = _parser.GetUInt8(); // valueInfoByte

				bool errorState = (dif & 0x30) == 0x30;
				byte difNorm = (byte)(dif & ~0x30); // dataInfoByteWithoutError


				// Energy (INT32)
				if (difNorm == 0x04 && TYPE_ENERGY.Contains(vif))
				{
					// Check if extra byte is used
					bool hasExtension = vif == 0xFB;
					byte? vifExt = null; // valueInfoExtraByte

					if (hasExtension)
						vifExt = _parser.GetUInt8(); 

					int raw = _parser.GetInt32();

					if (errorState)
						_decodingResult.AddWarning("Energy value is in error state");
					else if (hasExtension && vifExt != null)
						_decodingResult.AddResult("energy", ApplyEnergyExtensionScaling(raw, vifExt.Value), Unit.Megacalorie);
					else
						_decodingResult.AddResult("energy", ApplyEnergyScaling(raw, vif), GetEnergyUnit(vif));
				}

				// Volume (INT32)
				else if (difNorm == 0x04 && TYPE_VOLUME.Contains(vif))
				{
					int raw = _parser.GetInt32();
					if (errorState)
						_decodingResult.AddWarning("Volume value is in error state");
					else
					_decodingResult.AddResult("volume", ApplyVolumeScaling(raw, vif), Unit.CubicMeter);
				}

				// Power (INT16)
				else if (difNorm == 0x02 && TYPE_POWER.Contains(vif))
				{
					short raw = _parser.GetInt16();
					if (errorState)
						_decodingResult.AddWarning("Power value is in error state");
					else
						_decodingResult.AddResult("power", ApplyPowerScaling(raw, vif), GetPowerUnit(vif));
				}

				// Flow (INT16)
				else if (difNorm == 0x02 && TYPE_FLOW.Contains(vif))
				{
					short raw = _parser.GetInt16();
					if (errorState)
						_decodingResult.AddWarning("Flow value is in error state");
					else
						_decodingResult.AddResult("flow", ApplyFlowScaling(raw, vif), Unit.CubicMeterPerHour);
				}

				// Forward temperature (INT16)
				else if (difNorm == 0x02 && TYPE_FWTEMP.Contains(vif))
				{
					short raw = _parser.GetInt16();
					if (errorState)
						_decodingResult.AddWarning("Forward temperature value is in error state");
					else
						_decodingResult.AddResult("forwardTemperature", ApplyTemperatureScaling(raw, vif, 0x58), Unit.Celsius);
				}

				// Return temperature (INT16)
				else if (difNorm == 0x02 && TYPE_RTTEMP.Contains(vif))
				{
					short raw = _parser.GetInt16();
					if (errorState)
						_decodingResult.AddWarning("Return temperature value is in error state");
					else
						_decodingResult.AddResult("returnTemperature", ApplyTemperatureScaling(raw, vif, 0x5C), Unit.Celsius);
				}

				// Meter ID (INT32)
				else if (difNorm == 0x0C && vif == 0x78)
				{
					// Read 4 bytes in reverse order (little-endian)
					byte b0 = _parser.GetUInt8();
					byte b1 = _parser.GetUInt8();
					byte b2 = _parser.GetUInt8();
					byte b3 = _parser.GetUInt8();

					// Build meter ID by reading each nibble as a decimal digit
					string meterId = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}",
						b3 >> 4, b3 & 0x0F,
						b2 >> 4, b2 & 0x0F,
						b1 >> 4, b1 & 0x0F,
						b0 >> 4, b0 & 0x0F
					);

					_decodingResult.AddResult("meterId", meterId);
				}

				// Error codes (header: 01 FD 17, value: 1 byte)
				else if (difNorm == 0x01 && vif == 0xFD)
				{
					_parser.GetUInt8(); // Skip 0x17
					_decodingResult.AddResult("errorFlags", DecodeErrorFlags(_parser.GetUInt8()));
				}

				else
				{
					_decodingResult.AddWarning($"Unknown DIB: DIF=0x{dif:X2}, VIF=0x{vif:X2}");
				}
			}
		}

		// ─────────────────────────────────────────────
		// Error flag bit fields (from documentation).
		//
		// Bit 0 (0x01)  Temperature sensor 1 cable break
		// Bit 1 (0x02)  Temperature sensor 1 short circuit
		// Bit 2 (0x04)  Temperature sensor 2 cable break
		// Bit 3 (0x08)  Temperature sensor 2 short circuit
		// Bit 4 (0x10)  Error in flow measurement system
		// Bit 5 (0x20)  Electronics defective
		// Bit 6 (0x40)  Instrument has been reset
		// Bit 7 (0x80)  Low battery
		//
		// One payload can contain one or several error codes.
		// ─────────────────────────────────────────────

		private string DecodeErrorFlags(byte errorCode)
		{
			List<string> description = new List<string>();

			if ((errorCode & 0x01) != 0) description.Add("Temperature sensor 1 cable break");
			if ((errorCode & 0x02) != 0) description.Add("Temperature sensor 1 short circuit");
			if ((errorCode & 0x04) != 0) description.Add("Temperature sensor 2 cable break");
			if ((errorCode & 0x08) != 0) description.Add("Temperature sensor 2 short circuit");
			if ((errorCode & 0x10) != 0) description.Add("Error in flow measurement system");
			if ((errorCode & 0x20) != 0) description.Add("Electronics defective");
			if ((errorCode & 0x40) != 0) description.Add("Instrument has been reset");
			if ((errorCode & 0x80) != 0) description.Add("Low battery");
			
			return string.Join(", ", description);
		}

		private static double ApplyEnergyScaling(int raw, byte vif)
		{
			switch (vif)
			{
				case 0x00: return raw * 0.001;  // Wh × 0.001
				case 0x01: return raw * 0.01;   // Wh × 0.01
				case 0x02: return raw * 0.1;    // Wh × 0.1
				case 0x03: return raw * 1;      // Wh 
				case 0x04: return raw * 10;     // Wh × 10
				case 0x05: return raw * 100;    // Wh × 100
				case 0x06: return raw * 1;      // kWh
				case 0x07: return raw * 10;     // kWh × 10
				case 0x0E: return raw * 1;      // MJ
				case 0x0F: return raw * 10;		// MJ x 10
				default: return raw;
			}
		}

		private static double ApplyEnergyExtensionScaling(int raw, byte vif)
		{
			switch (vif)
			{
				case 0x0D: return raw * 1;		// MCal 
				case 0x0E: return raw * 10;		// MCal x 10
				case 0x0F: return raw * 100;	// MCal x 100
				default: return raw;
			}
		}

		private static Unit GetEnergyUnit(byte vif)
		{
			switch (vif)
			{
				case 0x00:
				case 0x01:
				case 0x02:
				case 0x03:
				case 0x04:
				case 0x05: 
					return Unit.WattHour;
				case 0x06:
				case 0x07:
					return Unit.KiloWattHour;
				case 0x0E:
				case 0x0F:
					return Unit.Megajoule;
				default: return Unit.WattHour;
			}
		}


		private static double ApplyVolumeScaling(int raw, byte vif)
		{
			switch (vif)
			{
				case 0x11: return raw * 0.00001;
				case 0x12: return raw * 0.0001;
				case 0x13: return raw * 0.001;
				case 0x14: return raw * 0.01;
				case 0x15: return raw * 0.1;
				case 0x16: return raw * 1.0;
				case 0x17: return raw * 10.0;
				default: return raw;
			}
		}

		private static double ApplyPowerScaling(short raw, byte vif)
		{
			switch (vif)
			{
				case 0x2B: return raw * 1;		// W
				case 0x2C: return raw * 10;		// W x 10
				case 0x2D: return raw * 100;	// W x 100
				case 0x2E: return raw * 1;      // kW
				case 0x2F: return raw * 10;		// kW x 10
				default: return raw;
			}
		}

		private static Unit GetPowerUnit(byte vif)
		{
			switch (vif)
			{
				case 0x2B:
				case 0x2C:
				case 0x2D:
					return Unit.Watt;
				case 0x2E: 
				case 0x2F:
					return Unit.KiloWatt;
				default:
					return Unit.Watt;
			}
		}

		private static double ApplyFlowScaling(short raw, byte vif)
		{
			// Returns flow in m³/h
			switch (vif)
			{
				case 0x3B: return raw * 0.001;  // m³/h x 0.001
				case 0x3C: return raw * 0.01;   // m³/h x 0.01
				case 0x3D: return raw * 0.1;    // m³/h x  0.1
				case 0x3E: return raw * 1;      // m³/h
				case 0x3F: return raw * 10;     // m³/h x 10
				default: return raw;
			}
		}

		private static double ApplyTemperatureScaling(short raw, byte vif, byte baseVif)
		{
			// Returns temperature in C
			int offset = vif - baseVif;
			switch (offset)
			{
				case 0: return raw * 0.001;     // °C x 0.001
				case 1: return raw * 0.01;      // °C x 0.01
				case 2: return raw * 0.1;       // °C x 0.1
				case 3: return raw * 1.0;       // °C
				default: return raw;
			}
			;
		}

	}
}