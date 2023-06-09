namespace LLama.Models
{
	public class LlamaToken
	{
		public LlamaToken(int id, string value)
		{
			this.Id = id;
			this.Value = value;
		}

		public int Id { get; private set; }

		public bool IsControl => this.Value is null;

		public string Value { get; private set; }

		public static bool operator !=(LlamaToken x, LlamaToken y) => !(x == y);

		public static bool operator ==(LlamaToken x, LlamaToken y) => x?.Id == y?.Id;

		public override bool Equals(object? obj) => obj is LlamaToken o && this == o;

		public override int GetHashCode() => this.Id;

		public override string ToString() => this.Value;
	}
}