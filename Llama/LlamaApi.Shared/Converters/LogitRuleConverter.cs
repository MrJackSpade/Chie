using Llama.Data.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Converters
{
    public class LogitRuleConverter : JsonConverter<LogitRule>
    {
        public override LogitRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);

            if (doc.RootElement.TryGetProperty(nameof(LogitRule.RuleType), out JsonElement ruleTypeProperty))
            {
                LogitRuleType ruleType = (LogitRuleType)ruleTypeProperty.GetInt32();

                string text = doc.RootElement.GetRawText();

                switch (ruleType)
                {
                    case LogitRuleType.Bias:
                        return JsonSerializer.Deserialize<LogitBias>(text, options);

                    case LogitRuleType.Clamp:
                        return JsonSerializer.Deserialize<LogitClamp>(text, options);

                    case LogitRuleType.Penalty:
                        return JsonSerializer.Deserialize<LogitPenalty>(text, options);
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, LogitRule value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}