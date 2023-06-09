namespace ChromaDbClient.Operators
{
	public class WhereDocument
	{
		public WhereDocument(WhereDocument key, string value)
		{
			this.Key = key;
			this.Value = value;
		}

		public WhereDocument(WhereDocument key, float value)
		{
			this.Key = key;
			this.Value = value;
		}

		public WhereDocument(WhereDocument key, WhereDocument value)
		{
			this.Key = key;
			this.Value = value;
		}

		public WhereDocument Key { get; set; }

		public object Value { get; set; }
	}
}