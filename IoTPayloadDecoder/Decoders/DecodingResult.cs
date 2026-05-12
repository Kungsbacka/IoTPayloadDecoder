using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders
{
    public class DecodingResult
    {
        private readonly bool _compact;
        // Could we have this as a dictionary from the start and just make dynamic when returning?
        private dynamic _result;
        private List<string> _warnings;

        public DecodingResult(bool compact)
        {
            _compact = compact;
            _result = new ExpandoObject();
            _warnings = new List<string>();
        }

        public void AddResult<T>(string name, T value, Unit unit)
        {
            var dictionary = (IDictionary<string, object>)_result;

            if (_compact)
            {
                dictionary[name] = value;
                return;
            }

            dynamic item = new ExpandoObject();
            item.value = value;
            item.unit = unit.ToUnitString();

            dictionary[name] = item;
        }

        public void AddResult<T>(string name, T value)
        {
            var dictionary = (IDictionary<string, object>)_result;

            if (_compact)
            {
                dictionary[name] = value;
                return;
            }

            dynamic item = new ExpandoObject();
            item.value = value;

            dictionary[name] = item;
        }

        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }

        public dynamic FinishResult()
        {
            _result.warnings = _warnings.ToArray();

            return _result;
        }
    }
}