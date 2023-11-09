namespace Llama.Data.Models
{
    public class LogitClamp : LogitRule
    {
        private float _startValue;

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
}