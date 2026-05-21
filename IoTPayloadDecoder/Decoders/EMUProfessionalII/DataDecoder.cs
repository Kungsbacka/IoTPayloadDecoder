using System;
using System.Collections.Generic;
using System.Text;

namespace IoTPayloadDecoder.Decoders.EMUProfessionalII
{
    public class DataDecoder : IPayloadDecoder
	{
		private DecodingResult _decodingResult;
		private PayloadParser _parser;

		public dynamic Decode(string payloadString, bool compact)
		{
			if (string.IsNullOrWhiteSpace(payloadString))
			{
				throw new ArgumentException("Payload string cannot be empty", nameof(payloadString));
			}

			// CRC-8 check
			byte[] payloadBytes = HexToBytes(payloadString);
			byte receivedCrc = payloadBytes[payloadBytes.Length - 1];
			byte calculatedCrc = Crc8(payloadBytes, payloadBytes.Length - 1);
			if (receivedCrc != calculatedCrc)
			{
				throw new PayloadParsingException($"CRC-8 mismatch: received 0x{receivedCrc:X2}, calculated 0x{calculatedCrc:X2}");
			}

			_parser = new PayloadParser(payloadString);
			_decodingResult = new DecodingResult(compact);

			DecodeData();

			return _decodingResult.FinishResult();
		}

		private void DecodeData()
		{
			// Bytes 0–3: timestamp
			DateTime timestamp = _parser.GetUnixEpoch();
			_decodingResult.AddResult("meterTimeUtc", timestamp);

			// Bytes 4–8: active energy import T1
			byte typeByte = _parser.GetUInt8();
			if (typeByte != 0x03)
				throw new PayloadParsingException(
					$"Expected type 0x03 (Active Energy Import T1), got 0x{typeByte:X2}");
			_decodingResult.AddResult("activeEnergyImportT1", ApplyEnergyScaling(_parser.GetUInt32()), Unit.KiloWattHour);

			// Bytes 9–13: active energy import T2
			typeByte = _parser.GetUInt8();
			if (typeByte != 0x04)
				throw new PayloadParsingException(
					$"Expected type 0x04 (Active Energy Import T2), got 0x{typeByte:X2}");
			_decodingResult.AddResult("activeEnergyImportT2", ApplyEnergyScaling(_parser.GetUInt32()), Unit.KiloWattHour);

			// Bytes 14–18: active energy export T1
			typeByte = _parser.GetUInt8();
			if (typeByte != 0x05)
				throw new PayloadParsingException(
					$"Expected type 0x05 (Active Energy Export T1), got 0x{typeByte:X2}");
			_decodingResult.AddResult("activeEnergyExportT1", ApplyEnergyScaling(_parser.GetUInt32()), Unit.KiloWattHour);

			// Bytes 19–23: active energy export T2
			typeByte = _parser.GetUInt8();
			if (typeByte != 0x06)
				throw new PayloadParsingException(
					$"Expected type 0x06 (Active Energy Export T2), got 0x{typeByte:X2}");
			_decodingResult.AddResult("activeEnergyExportT2", ApplyEnergyScaling(_parser.GetUInt32()), Unit.KiloWattHour);

			// Bytes 24–25: error code
			typeByte = _parser.GetUInt8();
			if (typeByte != 0xFF)
				throw new PayloadParsingException(
					$"Expected type 0xFF (Error code), got 0x{typeByte:X2}");
			byte errorCode = _parser.GetUInt8();
			_decodingResult.AddResult("errorCode", errorCode);
			_decodingResult.AddResult("errorCodeDescription", DecodeErrorCode(errorCode));
		}

		// ─────────────────────────────────────────────
		// Error code bit fields (from documentation)
		//
		// Bit 0  Time set
		// Bit 1  CT ratio adjusted
		// Bit 2  VT ratio adjusted
		// Bit 3  Impulse length adjusted
		// Bit 4  Impulse ratio adjusted
		// Bit 5  Voltage interruption
		// Bit 6  Time not valid or not synchronized
		// Bit 7  Logbook full
		// ─────────────────────────────────────────────

		private string DecodeErrorCode(byte errorCode)
		{
			List<string> description = new List<string>();

			if ((errorCode & 0x01) != 0)
				description.Add("errTimeSet");

			if ((errorCode & 0x02) != 0)
				description.Add("errCTRatioAdjusted");

			if ((errorCode & 0x04) != 0)
				description.Add("errVTRatioAdjusted");

			if ((errorCode & 0x08) != 0)
				description.Add("errImpulseLengthAdjusted");

			if ((errorCode & 0x10) != 0)
				description.Add("errImpulseRatioAdjusted");

			if ((errorCode & 0x20) != 0)
				description.Add("errVoltageInterruption");

			if ((errorCode & 0x40) != 0)
				description.Add("errTimeNotSynchronized");

			if ((errorCode & 0x80) != 0)
				description.Add("errLogbookFull");

			return string.Join(", ", description);
		}

		// ─────────────────────────────────────────────
		// Converts hex string to byte array for CRC calculation.
		// ─────────────────────────────────────────────
		private static byte[] HexToBytes(string hex)
		{
			byte[] bytes = new byte[hex.Length / 2];
			for (int i = 0; i < bytes.Length; i++)
				bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
			return bytes;
		}

		// ─────────────────────────────────────────────
		// CRC-8 (polynomial x^8 + x^2 + x^1 + x^0)
		// ─────────────────────────────────────────────
		private static readonly byte[] _crc8Table =
		{
			0x00,0x07,0x0E,0x09,0x1C,0x1B,0x12,0x15,0x38,0x3F,0x36,0x31,0x24,0x23,0x2A,0x2D,
			0x70,0x77,0x7E,0x79,0x6C,0x6B,0x62,0x65,0x48,0x4F,0x46,0x41,0x54,0x53,0x5A,0x5D,
			0xE0,0xE7,0xEE,0xE9,0xFC,0xFB,0xF2,0xF5,0xD8,0xDF,0xD6,0xD1,0xC4,0xC3,0xCA,0xCD,
			0x90,0x97,0x9E,0x99,0x8C,0x8B,0x82,0x85,0xA8,0xAF,0xA6,0xA1,0xB4,0xB3,0xBA,0xBD,
			0xC7,0xC0,0xC9,0xCE,0xDB,0xDC,0xD5,0xD2,0xFF,0xF8,0xF1,0xF6,0xE3,0xE4,0xED,0xEA,
			0xB7,0xB0,0xB9,0xBE,0xAB,0xAC,0xA5,0xA2,0x8F,0x88,0x81,0x86,0x93,0x94,0x9D,0x9A,
			0x27,0x20,0x29,0x2E,0x3B,0x3C,0x35,0x32,0x1F,0x18,0x11,0x16,0x03,0x04,0x0D,0x0A,
			0x57,0x50,0x59,0x5E,0x4B,0x4C,0x45,0x42,0x6F,0x68,0x61,0x66,0x73,0x74,0x7D,0x7A,
			0x89,0x8E,0x87,0x80,0x95,0x92,0x9B,0x9C,0xB1,0xB6,0xBF,0xB8,0xAD,0xAA,0xA3,0xA4,
			0xF9,0xFE,0xF7,0xF0,0xE5,0xE2,0xEB,0xEC,0xC1,0xC6,0xCF,0xC8,0xDD,0xDA,0xD3,0xD4,
			0x69,0x6E,0x67,0x60,0x75,0x72,0x7B,0x7C,0x51,0x56,0x5F,0x58,0x4D,0x4A,0x43,0x44,
			0x19,0x1E,0x17,0x10,0x05,0x02,0x0B,0x0C,0x21,0x26,0x2F,0x28,0x3D,0x3A,0x33,0x34,
			0x4E,0x49,0x40,0x47,0x52,0x55,0x5C,0x5B,0x76,0x71,0x78,0x7F,0x6A,0x6D,0x64,0x63,
			0x3E,0x39,0x30,0x37,0x22,0x25,0x2C,0x2B,0x06,0x01,0x08,0x0F,0x1A,0x1D,0x14,0x13,
			0xAE,0xA9,0xA0,0xA7,0xB2,0xB5,0xBC,0xBB,0x96,0x91,0x98,0x9F,0x8A,0x8D,0x84,0x83,
			0xDE,0xD9,0xD0,0xD7,0xC2,0xC5,0xCC,0xCB,0xE6,0xE1,0xE8,0xEF,0xFA,0xFD,0xF4,0xF3
		};

		// ─────────────────────────────────────────────
		// Calculates a CRC-8 checksum using the polynomial x^8 + x^2 + x^1 + x^0 (CRC-8/SMBUS).
		// ─────────────────────────────────────────────
		private static byte Crc8(byte[] data, int length)
		{
			byte crc = 0;
			for (int j = 0; j < length; j++)
				crc = _crc8Table[crc ^ data[j]];
			return crc;
		}

		// ─────────────────────────────────────────────
		// Raw value is in Wh; divide by 1000 to convert to kWh.
		// ─────────────────────────────────────────────
		private static double ApplyEnergyScaling(uint raw)
		{
			return raw * 0.001;
		}
	}
}
