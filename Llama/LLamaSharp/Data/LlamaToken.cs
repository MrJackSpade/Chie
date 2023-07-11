using Llama.Constants;
using Llama.Utilities;
using System;
using System.Diagnostics;

namespace Llama.Data
{
    [DebuggerDisplay("[{Tag}] {Value}")]
    public class LlamaToken
    {
        public LlamaToken(int id, IntPtr value, string tag)
        {
            this.Tag = tag;

            this.Id = id;

            this.Value = Utils.PtrToStringUTF8(value);
        }

        public static LlamaToken BOS => new(1, IntPtr.Zero, LlamaTokenTags.CONTROL);

        public static LlamaToken EOS => new(2, IntPtr.Zero, LlamaTokenTags.CONTROL);

        public static LlamaToken Null => new(0, IntPtr.Zero, LlamaTokenTags.NULL);

        public byte[] Bytes { get; private set; } = Array.Empty<byte>();

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

        public string Tag { get; private set; } = string.Empty;

        public string Value { get; private set; }

        public static int GetUTF8CharacterLength(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException("Invalid byte array.");
            }

            byte firstByte = bytes[0];

            if (firstByte >> 7 == 0)
            {
                return 1; // 0xxx xxxx
            }
            else if (firstByte >> 5 == 0b110)
            {
                return 2; // 110x xxxx
            }
            else if (firstByte >> 4 == 0b1110)
            {
                return 3; // 1110 xxxx
            }
            else if (firstByte >> 3 == 0b11110)
            {
                return 4; // 1111 0xxx
            }
            else
            {
                throw new ArgumentException("Invalid UTF-8 byte.");
            }
        }

        public static bool operator !=(LlamaToken x, LlamaToken y) => !(x == y);

        public static bool operator ==(LlamaToken x, LlamaToken y) => x?.Id == y?.Id;

        public override bool Equals(object? obj) => obj is LlamaToken o && this == o;

        public override int GetHashCode() => this.Id;

        public override string ToString() => this.Value;
    }
}