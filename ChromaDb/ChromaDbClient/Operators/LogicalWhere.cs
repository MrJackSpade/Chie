using ChromaDbClient.Interfaces;

namespace ChromaDbClient.Operators
{
	public class LogicalWhere : IWhere
	{
		public string Key { get; private set; }
		public IWhere Value { get; private set; }

		public LogicalWhere(string key, IWhere value)
		{
			this.Key = key;
			this.Value = value;
		}
	}
}
