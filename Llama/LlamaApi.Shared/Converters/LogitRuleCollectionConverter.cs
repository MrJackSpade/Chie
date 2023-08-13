using Llama.Data.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Converters
{
    public class LogitRuleCollectionConverter : JsonConverter<LogitRuleCollection>
    {
        public override LogitRuleCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            LogitRuleCollection collection = new();

            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    LogitRule? rule = JsonSerializer.Deserialize<LogitRule>(element.GetRawText(), options);
                    collection.Add(rule);
                }
            }

            return collection;
        }

        public override void Write(Utf8JsonWriter writer, LogitRuleCollection value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value as IEnumerable<LogitRule>, options);
    }
}