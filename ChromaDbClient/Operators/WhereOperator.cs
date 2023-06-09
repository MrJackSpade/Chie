using ChromaDbClient.Interfaces;

namespace ChromaDbClient.Operators
{
	public class WhereOperator : IOperator
	{
		private WhereOperator(string value)
		{
			this.Value = value;
		}

		public static WhereOperator Equal => new("$eg");

		public static WhereOperator GreaterThan => new("$gt");

		public static WhereOperator GreaterThanOrEqual => new("$gte");

		public static WhereOperator LessThan => new("$lt");

		public static WhereOperator LessThanOrEqual => new("$lte");

		public static WhereOperator NotEqual => new("$nq");

		public string Value { get; private set; }
	}
}