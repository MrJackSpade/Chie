using System.Text.Json.Serialization;

namespace Llama.Data.Models
{
    public class LogitBias : LogitRule
    {
        public LogitBias(int id, float value, LogitRuleLifetime lifeTime, LogitBiasType logitBiasType)
        {
            this.LifeTime = lifeTime;
            this.LogitId = id;
            this.Value = value;
            this.LogitBiasType = logitBiasType;
        }

        [JsonConstructor]
        public LogitBias()
        { }

        public LogitBiasType LogitBiasType { get; set; }

        public override LogitRuleType RuleType => LogitRuleType.Bias;

        public float Value { get; set; }

        public override string ToString() => $"[Bias] {this.Value}";
    }
}