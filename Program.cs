// See https://aka.ms/new-console-template for more information
using IoTPayloadDecoder;
using System.Text.Json;


string payloadString = "[payload]";
dynamic data = ElsysDecoder.Decode(payloadString);

string json = JsonSerializer.Serialize<dynamic>(data,
new JsonSerializerOptions()
{
    WriteIndented = true
});

Console.WriteLine(json);
