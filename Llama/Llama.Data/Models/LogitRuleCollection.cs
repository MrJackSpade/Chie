using Llama.Data.Extensions;
using System.Collections;

namespace Llama.Data.Models
{
    public class LogitRuleCollection : IEnumerable<LogitRule>
    {
        private readonly Dictionary<string, LogitRule> _keyValuePairs = new();

        public LogitRuleCollection()
        {
        }

        private LogitRuleCollection(IEnumerable<LogitRule> toClone)
        {
            this.Add(toClone);
        }

        public void Add(IEnumerable<LogitRule> rules)
        {
            foreach (LogitRule rule in rules)
            {
                this.Add(rule);
            }
        }

        public void Add(LogitRule rule) => this._keyValuePairs.Add(rule.Key, rule);

        public void AddOrUpdate(IEnumerable<LogitRule> rules)
        {
            foreach (LogitRule rule in rules)
            {
                this.AddOrUpdate(rule);
            }
        }

        public void AddOrUpdate(LogitRule rule) => this._keyValuePairs.AddOrUpdate(rule.Key, rule);

        public LogitRuleCollection Clone() => new(this);

        public IEnumerator<LogitRule> GetEnumerator() => this._keyValuePairs.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this._keyValuePairs.Values.GetEnumerator();

        public IEnumerable<T> OfType<T>() where T : LogitRule => this._keyValuePairs.Values.OfType<T>();

        public void Remove(LogitRuleLifetime lifetime)
        {
            HashSet<string> toRemove = new();

            foreach (LogitRule rule in this)
            {
                if (rule.LifeTime == lifetime)
                {
                    toRemove.Add(rule.Key);
                }
            }

            foreach (string key in toRemove)
            {
                this._keyValuePairs.Remove(key);
            }
        }

        public void Remove(string key) => this._keyValuePairs.Remove(key);
    }
}