using Llama.Data.Enums;
using System.Diagnostics;

namespace Llama.Data.Models
{
    [DebuggerDisplay("[{Tag}] {Value}")]
    public class LlamaToken
    {
        public LlamaToken(int id, string? value, LlamaTokenType tokenType = LlamaTokenType.Undefined)
        {
            this.TokenType = tokenType;

            this.Id = id;

            this.Value = value;
        }

        public static LlamaToken BOS => new(1, null, LlamaTokenType.Control);

        public static LlamaToken EOS => new(2, null, LlamaTokenType.Control);

        public static LlamaToken NewLine => new(13, null, LlamaTokenType.Null);

        public static LlamaToken Null => new(0, null, LlamaTokenType.Null);

        public byte[] Bytes { get; private set; } = Array.Empty<byte>();

        public object Data { get; set; }

        public string EscapedValue
        {
            get
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
        }

        public int Id { get; private set; }

        public bool IsControl => this.Value is null;

        public LlamaTokenType TokenType { get; private set; }

        public string? Value { get; private set; }

        public static bool operator !=(LlamaToken x, LlamaToken y) => !(x == y);

        public static bool operator ==(LlamaToken x, LlamaToken y) => x?.Id == y?.Id;

        public override bool Equals(object? obj) => obj is LlamaToken o && this == o;

        public override int GetHashCode() => this.Id;

        public override string? ToString() => this.Value;
    }
}