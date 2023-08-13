using System.Text.Json.Serialization;

namespace Llama.Data.Models
{
    public enum LogitClampType
    {
        PreventIncrease,

        PreventDecrease,

        PreventChange
    }

    public enum LogitRuleLifetime
    {
        Token,

        Inferrence,

        Context
    }

    public enum LogitRuleType
    {
        Bias,

        Clamp,

        Penalty
    }

    public class LogitBias : LogitRule
    {
        public LogitBias(int id, float value, LogitRuleLifetime lifeTime)
        {
            this.LifeTime = lifeTime;
            this.LogitId = id;
            this.Value = value;
        }

        [JsonConstructor]
        public LogitBias()
        { }

        public override LogitRuleType RuleType => LogitRuleType.Bias;

        public float Value { get; set; }

        public override string ToString() => $"[Bias] {this.Value}";
    }

    public class LogitClamp : LogitRule
    {
        private float _startValue;

        [JsonConstructor]
        public LogitClamp()
        { }

        public override LogitRuleType RuleType => LogitRuleType.Clamp;

        public LogitClampType Type { get; set; }

        public float GetValue(float newValue)
        {
            return this.Type switch
            {
                LogitClampType.PreventChange => this._startValue,
                LogitClampType.PreventDecrease => Math.Max(this._startValue, newValue),
                LogitClampType.PreventIncrease => Math.Min(this._startValue, newValue),
                _ => throw new NotImplementedException(),
            };
        }

        public void SetStart(float value) => this._startValue = value;

        public override string ToString() => $"[Clamp] {this.RuleType}";
    }

    public class LogitPenalty : LogitRule
    {
        public LogitPenalty(int id, float value, LogitRuleLifetime lifeTime)
        {
            this.LifeTime = lifeTime;
            this.LogitId = id;
            this.Value = value;
        }

        [JsonConstructor]
        public LogitPenalty()
        { }

        public override LogitRuleType RuleType => LogitRuleType.Penalty;

        public float Value { get; set; }

        public override string ToString() => $"[Penalty] {this.Value}";
    }

    public abstract class LogitRule
    {
        public string Key => $"{(int)this.RuleType}:{this.LogitId}";

        public LogitRuleLifetime LifeTime { get; set; }

        public int LogitId { get; set; }

        public abstract LogitRuleType RuleType { get; }
    }
}