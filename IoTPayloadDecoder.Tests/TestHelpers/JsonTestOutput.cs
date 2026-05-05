using System.Text.Json;
using Xunit.Abstractions;

namespace IoTPayloadDecoder.Tests.TestHelpers
{
    internal static class TestOutput
    {
        public static void PrintResult(ITestOutputHelper output, dynamic result)
        {
            string json = JsonSerializer.Serialize(
                result,
                new JsonSerializerOptions { WriteIndented = true });

            output.WriteLine(json);
        }
    }
}