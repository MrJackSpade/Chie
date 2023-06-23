using ChromaDbClient.Interfaces;

namespace ChromaDbClient.Models
{
	public class OperatorExpression
	{
		public IOperator Key { get; private set; }

		public object Value { get; private set; }

		public OperatorExpression(IOperator key, string value)
		{
			this.Value = value;
			this.Key = key;
		}
		public OperatorExpression(IOperator key, float value)
		{
			this.Value = value;
			this.Key = key;
		}
	}
}
