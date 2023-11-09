namespace Llama.Data.Models
{
    public class LogitPenalty : LogitRule
    {
        public LogitPenalty(int id, float value, LogitRuleLifetime lifeTime)
        {
            this.LifeTime = lifeTime;
            this.LogitId = id;
            this.Value = value;
        }

        public LogitPenalty()
        { }

        public override LogitRuleType RuleType => LogitRuleType.Penalty;

        public float Value { get; set; }

        public override string ToString() => $"[Penalty] {this.Value}";
    }
}