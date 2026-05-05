using System.Collections.Generic;
using System.Dynamic;

namespace IoTPayloadDecoder.Decoders
{
    public abstract class PayloadDecoderBase
    {
        private bool _compact;
        protected dynamic Result { get; private set; }
        protected List<string> Warnings { get; private set; }

        protected void InitResult(bool compact)
        {
            _compact = compact;
            Result = new ExpandoObject();
            Warnings = new List<string>();
        }

        protected void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        protected void AddResult<T>(string name, T value, Unit unit)
        {
            var dictionary = (IDictionary<string, object>)Result;

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

        protected void AddResult<T>(string name, T value)
        {
            var dictionary = (IDictionary<string, object>)Result;

            if (_compact)
            {
                dictionary[name] = value;
                return;
            }

            dynamic item = new ExpandoObject();
            item.value = value;

            dictionary[name] = item;
        }

        protected dynamic FinishResult()
        {
            Result.warnings = Warnings.ToArray();
            return Result;
        }
    }
}