using Llama.Data.Native;
using System.Runtime.InteropServices;

namespace Llama.Native
{
    internal unsafe partial class LlamaCppApi
    {
        /// <summary>
        /// Frequency and presence penalties described in OpenAI API https://platform.openai.com/docs/api-reference/parameter-details.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="last_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="alpha_frequency"></param>
        /// <param name="alpha_presence"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_frequency_and_presence_penalties")]
        public static extern void SampleFrequencyAndPresencePenalties(SafeLlamaContextHandle ctx, IntPtr candidates, int[] last_tokens, ulong last_tokens_size, float alpha_frequency, float alpha_presence);

        /// <summary>
        /// Repetition penalty described in CTRL academic paper https://arxiv.org/abs/1909.05858, with negative logit fix.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="last_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="penalty"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_repetition_penalty")]
        public static extern void SampleRepetitionPenalty(SafeLlamaContextHandle ctx, IntPtr candidates, int[] last_tokens, ulong last_tokens_size, float penalty);

        /// <summary>
        /// Repetition penalty described in CTRL academic paper https://arxiv.org/abs/1909.05858, with negative logit fix.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="last_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="penalty"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_complex_presence")]
        public static extern void ComplexPresencePenalty(SafeLlamaContextHandle ctx, IntPtr candidates, int[] last_tokens, ulong last_tokens_size, int minGroupLength, float scalePerGroup, float scalePerLength);

        /// <summary>
        /// Sorts candidate tokens by their logits in descending order and calculate probabilities based on logits.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_softmax")]
        public static extern void SampleSoftmax(SafeLlamaContextHandle ctx, IntPtr candidates);

        /// <summary>
        /// Tail Free Sampling described in https://www.trentonbricken.com/Tail-Free-Sampling/.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="z"></param>
        /// <param name="min_keep"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_tail_free")]
        public static extern void SampleTailFree(SafeLlamaContextHandle ctx, IntPtr candidates, float z, ulong min_keep);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_temperature")]
        public static extern void SampleTemperature(SafeLlamaContextHandle ctx, IntPtr candidates, float temp);

        /// <summary>
        /// Randomly selects a token from the candidates based on their probabilities.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_token")]
        public static extern int SampleToken(SafeLlamaContextHandle ctx, IntPtr candidates);

        /// <summary>
        /// Selects the token with the highest probability.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_token_greedy")]
        public static extern int SampleTokenGreedy(SafeLlamaContextHandle ctx, IntPtr candidates);

        /// <summary>
        /// Mirostat 1.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `int_data` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="m">The number of tokens considered in the estimation of `s_hat`. This is an arbitrary value that is used to calculate `s_hat`, which in turn helps to calculate the value of `k`. In the paper, they use `m = 100`, but you can experiment with different values to see how it affects the performance of the algorithm.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_token_mirostat")]
        public static extern int SampleTokenMirostat(SafeLlamaContextHandle ctx, IntPtr candidates, float tau, float eta, int m, float* mu);

        /// <summary>
        /// Mirostat 2.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `int_data` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_token_mirostat_v2")]
        public static extern int SampleTokenMirostatV2(SafeLlamaContextHandle ctx, IntPtr candidates, float tau, float eta, float* mu);

        /// <summary>
        /// Top-K sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="k"></param>
        /// <param name="min_keep"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_top_k")]
        public static extern void SampleTopK(SafeLlamaContextHandle ctx, IntPtr candidates, int k, ulong min_keep);

        /// <summary>
        /// Nucleus sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_top_p")]
        public static extern void SampleTopP(SafeLlamaContextHandle ctx, IntPtr candidates, float p, ulong min_keep);

        /// <summary>
        /// Locally Typical Sampling implementation described in the paper https://arxiv.org/abs/2202.00666.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_sample_typical")]
        public static extern void SampleTypical(SafeLlamaContextHandle ctx, IntPtr candidates, float p, ulong min_keep);
    }
}