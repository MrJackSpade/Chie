using System.Buffers.Binary;
using System.Text.Json.Serialization;

namespace LlamaApi.Shared.Models.Response
{
    public class GetLogitsResponse
    {
        private const int FLOAT_SIZE = sizeof(float);

        [JsonPropertyName("data")]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public IEnumerable<float> GetValue()
        {
            if (this.Data.Length % FLOAT_SIZE != 0)
            {
                throw new ArgumentException("Invalid data length", nameof(this.Data));
            }

            for (int i = 0; i < this.Data.Length; i += FLOAT_SIZE)
            {
                yield return BitConverter.ToSingle(this.Data, i);
            }
        }

        public void SetValue(Span<float> floatValues)
        {
            if (floatValues == null)
            {
                throw new ArgumentNullException(nameof(floatValues));
            }

            this.Data = new byte[floatValues.Length * FLOAT_SIZE];

            Span<byte> resultSpan = this.Data;

            for (int i = 0; i < floatValues.Length; i++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(resultSpan[(i * FLOAT_SIZE)..], floatValues[i]);
            }
        }
    }
}