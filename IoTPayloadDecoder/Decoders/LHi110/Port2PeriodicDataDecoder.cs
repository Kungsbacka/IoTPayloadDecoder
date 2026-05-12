using System;

namespace IoTPayloadDecoder.Decoders.LHi110
{
    public class Port2PeriodicDataDecoder : IPayloadDecoder
    {
        private PayloadParser _parser;
        private DecodingResult _decodingResult;

        public dynamic Decode(string payloadString, bool compact)
        {
            if (string.IsNullOrWhiteSpace(payloadString))
            {
                throw new ArgumentException("Payload string cannot be empty", nameof(payloadString));
            }

            _parser = new PayloadParser(payloadString);
            _decodingResult = new DecodingResult(compact);

            DecodePeriodicData();

            return _decodingResult.FinishResult();

        }

        private void DecodePeriodicData()
        {
            byte messageFormat = _parser.GetUInt8();
            _decodingResult.AddResult("messageFormat", messageFormat, Unit.Count);

            //Oberoende av messageType, kommer med alla typer 1-9
            _decodingResult.AddResult("meterTimeUtc", _parser.GetUnixEpochBE());
  
            switch (messageFormat)
            {
                case 0x01:
                    DecodeReportFormat1();
                    break;

                default:
                    _decodingResult.AddWarning($"Unsupported message format: 0x{messageFormat:X2}");
                    break;
            }
        }

        private void DecodeReportFormat1()
        {
            _decodingResult.AddResult("activeImportReading", _parser.GetUInt40BE(), Unit.WattHour);

            _decodingResult.AddResult("actL1ImportPowerPeak", DecodePower(_parser.GetUInt16BE()), Unit.Watt);
            _decodingResult.AddResult("actL1ImportPowerAver", DecodePower(_parser.GetUInt16BE()), Unit.Watt);
            _decodingResult.AddResult("actL2ImportPowerPeak", DecodePower(_parser.GetUInt16BE()), Unit.Watt);
            _decodingResult.AddResult("actL2ImportPowerAver", DecodePower(_parser.GetUInt16BE()), Unit.Watt);
            _decodingResult.AddResult("actL3ImportPowerPeak", DecodePower(_parser.GetUInt16BE()), Unit.Watt);
            _decodingResult.AddResult("actL3ImportPowerAver", DecodePower(_parser.GetUInt16BE()), Unit.Watt);

            uint voltagePacked = _parser.GetUInt32BE();
            int[] voltages = DecodeVoltages(voltagePacked);

            _decodingResult.AddResult("l1VoltageAver", voltages[0], Unit.Millivolt);
            _decodingResult.AddResult("l2VoltageAver", voltages[1], Unit.Millivolt);
            _decodingResult.AddResult("l3VoltageAver", voltages[2], Unit.Millivolt);

            uint currentPacked = _parser.GetUInt32BE();
            long[] currents = DecodeCurrents(currentPacked);

            _decodingResult.AddResult("l1CurrentPeak", currents[0], Unit.Milliampere);
            _decodingResult.AddResult("l2CurrentPeak", currents[1], Unit.Milliampere);
            _decodingResult.AddResult("l3CurrentPeak", currents[2], Unit.Milliampere);
        }

        // ===== Helpers =====
        private static int DecodePower(ushort rawValue)
        {
            // Om högsta biten (bit 15) är satt så skall värdet skalas upp med 100.
            bool useHundreds = (rawValue & (1 << 15)) != 0;

            if (useHundreds)
            {
                return (rawValue & ~(1 << 15)) * 100;
            }

            return rawValue;
        }

        private static int[] DecodeVoltages(uint packed)
        {
            // L1, L2 och L3 ligger ihoppackade på ett gemensamt 32-bitars tal.
            // Binärtalet kan visualiseras enligt nedan:
            // [ 2 oanvända bitar ][ L1: 10 bitar ][ L2: 10 bitar ][ L3: 10 bitar ]
            int l1Raw = (int)((packed & 0x3FF00000) >> 20);
            int l2Raw = (int)((packed & 0x000FFC00) >> 10);
            int l3Raw = (int)(packed & 0x000003FF);

            return new[]
            {
                DecodeVoltage(l1Raw),
                DecodeVoltage(l2Raw),
                DecodeVoltage(l3Raw)
            };
        }

        private static int DecodeVoltage(int raw)
        {
            // Om råvärdet är 0 så skall ingen offset läggas på. Om råvärdet är större än 0
            // så skall råvärdet skalas med faktor 250, och adderas med en offset på 20000 (mV)
            if (raw == 0)
            {
                return 0;
            }

            return raw * 250 + 20000;
        }

        private static long[] DecodeCurrents(uint packed)
        {
            //TODO: Verify with supplier whether bits 31-30 should be used as current scaleIndex.

            //int scaleIndex = (int)((packed & 0xC0000000) >> 30);
            //long scaleFactor = (long)Math.Pow(10, scaleIndex);

            long l1 = ((packed & 0x3FF00000) >> 20) * 100L;
            long l2 = ((packed & 0x000FFC00) >> 10) * 100L;
            long l3 = (packed & 0x000003FF) * 100L;

            return new[] { l1, l2, l3 };
        }
    }
}