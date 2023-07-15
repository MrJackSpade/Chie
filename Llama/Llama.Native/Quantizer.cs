using Llama.Data.Native;

namespace Llama.Native
{
    public class Quantizer
    {
        /// <summary>
        /// Quantize the model.
        /// </summary>
        /// <param name="srcFileName">The model file to be quantized.</param>
        /// <param name="dstFilename">The path to save the quantized model.</param>
        /// <param name="ftype">The type of quantization.</param>
        /// <param name="nthread">Thread to be used during the quantization. By default it's the physical core number.</param>
        /// <returns>Whether the quantization is successful.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool Quantize(string srcFileName, string dstFilename, LlamaFtype ftype, int nthread = -1)
        {
            if (!ValidateFtype(ftype))
            {
                throw new ArgumentException($"The type {Enum.GetName(typeof(LlamaFtype), ftype)} is not a valid type " +
                    $"to perform quantization.");
            }

            return LlamaCppApi.ModelQuantize(srcFileName, dstFilename, ftype, nthread) == 0;
        }

        /// <summary>
        /// Quantize the model.
        /// </summary>
        /// <param name="srcFileName">The model file to be quantized.</param>
        /// <param name="dstFilename">The path to save the quantized model.</param>
        /// <param name="ftype">The type of quantization.</param>
        /// <param name="nthread">Thread to be used during the quantization. By default it's the physical core number.</param>
        /// <returns>Whether the quantization is successful.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool Quantize(string srcFileName, string dstFilename, string ftype, int nthread = -1) => Quantize(srcFileName, dstFilename, StringToFtype(ftype), nthread);

        private static string FtypeToString(LlamaFtype ftype)
        {
            return ftype switch
            {
                LlamaFtype.LLAMA_FTYPE_MOSTLY_Q4_0 => "q4_0",
                LlamaFtype.LLAMA_FTYPE_MOSTLY_Q4_1 => "q4_1",
                LlamaFtype.LLAMA_FTYPE_MOSTLY_Q5_0 => "q5_0",
                LlamaFtype.LLAMA_FTYPE_MOSTLY_Q5_1 => "q5_1",
                LlamaFtype.LLAMA_FTYPE_MOSTLY_Q8_0 => "q8_0",
                _ => throw new ArgumentException($"The type {Enum.GetName(typeof(LlamaFtype), ftype)} is not a valid type " +
                    $"to perform quantization.")
            };
        }

        private static LlamaFtype StringToFtype(string str)
        {
            return str switch
            {
                "q4_0" => LlamaFtype.LLAMA_FTYPE_MOSTLY_Q4_0,
                "q4_1" => LlamaFtype.LLAMA_FTYPE_MOSTLY_Q4_1,
                "q5_0" => LlamaFtype.LLAMA_FTYPE_MOSTLY_Q5_0,
                "q5_1" => LlamaFtype.LLAMA_FTYPE_MOSTLY_Q5_1,
                "q8_0" => LlamaFtype.LLAMA_FTYPE_MOSTLY_Q8_0,
                _ => throw new NotImplementedException(),
            };
        }

        private static bool ValidateFtype(string ftype) => new string[] { "q4_0", "q4_1", "q5_0", "q5_1", "q8_0" }.Contains(ftype);

        private static bool ValidateFtype(LlamaFtype ftype)
        {
            return ftype is LlamaFtype.LLAMA_FTYPE_MOSTLY_Q4_0 or LlamaFtype.LLAMA_FTYPE_MOSTLY_Q4_1
                or LlamaFtype.LLAMA_FTYPE_MOSTLY_Q5_0 or LlamaFtype.LLAMA_FTYPE_MOSTLY_Q5_1 or LlamaFtype.LLAMA_FTYPE_MOSTLY_Q8_0;
        }
    }
}