using System.Diagnostics;

namespace Llama.Data.Models
{
    [DebuggerDisplay("{Value}")]
    public class LlamaToken
    {
        public LlamaToken(int id, string? value)
        {
            this.Id = id;

            this.Value = value;
        }

        public static LlamaToken BOS => new(1, null);

        public static LlamaToken EOS => new(2, null);

        public static LlamaToken NewLine => new(13, null);

        public static LlamaToken Null => new(0, "█");

        public int Id { get; private set; }

        public string? Value { get; private set; }

        public static bool operator !=(LlamaToken x, LlamaToken y) => !(x == y);

        public static bool operator ==(LlamaToken x, LlamaToken y) => x?.Id == y?.Id;

        public override bool Equals(object? obj) => obj is LlamaToken o && this == o;

        public string? GetEscapedValue()
        {
            switch (this.Id)
            {
                case 1:
                    return "[BOS]";

                case 2:
                    return "[EOS]";

                default:
                    break;
            }

            return this.Value switch
            {
                "\r" => "\\r",
                "\n" => "\\n",
                _ => this.Value,
            };
        }

        public override int GetHashCode() => this.Id;

        public override string? ToString() => this.Value;
    }
}