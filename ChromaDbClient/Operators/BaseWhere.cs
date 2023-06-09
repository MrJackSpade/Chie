using ChromaDbClient.Interfaces;
using ChromaDbClient.Models;

namespace ChromaDbClient.Operators
{
	public class BaseWhere : IWhere
	{
		public object Value { get; private set; }
		public string Key { get; private set; }
		public BaseWhere(string key, string value)
		{
			this.Key = key;
			this.Value = value;
		}
		public BaseWhere(string key, float value)
		{
			this.Key = key;
			this.Value = value;
		}
		public BaseWhere(string key, OperatorExpression value)
		{
			this.Key = key;
			this.Value = value;
		}
	}
}
