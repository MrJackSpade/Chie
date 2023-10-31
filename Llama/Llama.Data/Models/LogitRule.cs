namespace Llama.Data.Models
{
    public abstract class LogitRule
    {
        public string Key => $"{(int)this.RuleType}:{this.LogitId}";

        public LogitRuleLifetime LifeTime { get; set; }

        public int LogitId { get; set; }

        public abstract LogitRuleType RuleType { get; }
    }
}