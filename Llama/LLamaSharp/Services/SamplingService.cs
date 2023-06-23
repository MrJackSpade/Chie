using Llama.Context.Interfaces;
using Llama.Native;
using Llama.Native.Data;
using LLama.Native;
using System;

namespace Llama.Services
{
    internal unsafe class SamplingService
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
        public static void FrequencyAndPresencePenalties(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, int[] last_tokens, ulong last_tokens_size, float alpha_frequency, float alpha_presence)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };

            NativeApi.llama_sample_frequency_and_presence_penalties(ctx, new IntPtr(&st), last_tokens, last_tokens_size, alpha_frequency, alpha_presence);
        }

        /// <summary>
        /// Repetition penalty described in CTRL academic paper https://arxiv.org/abs/1909.05858, with negative logit fix.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="last_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="penalty"></param>
        public static void RepetitionPenalty(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, int[] last_tokens, ulong last_tokens_size, float penalty)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            NativeApi.llama_sample_repetition_penalty(ctx, new IntPtr(&st), last_tokens, last_tokens_size, penalty);
        }

        /// <summary>
        /// Sorts candidate tokens by their logits in descending order and calculate probabilities based on logits.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        public static void SoftMax(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            NativeApi.llama_sample_softmax(ctx, new IntPtr(&st));
        }

        /// <summary>
        /// Tail Free Sampling described in https://www.trentonbricken.com/Tail-Free-Sampling/.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="z"></param>
        /// <param name="min_keep"></param>
        public static void TailFree(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, float z, ulong min_keep)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            NativeApi.llama_sample_tail_free(ctx, new IntPtr(&st), z, min_keep);
        }

        public static void Temperature(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, float temp)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            NativeApi.llama_sample_temperature(ctx, new IntPtr(&st), temp);
        }

        /// <summary>
        /// Randomly selects a token from the candidates based on their probabilities.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <returns></returns>
        public static int Token(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            return NativeApi.llama_sample_token(ctx, new IntPtr(&st));
        }

        /// <summary>
        /// Selects the token with the highest probability.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <returns></returns>
        public static int TokenGreedy(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            return NativeApi.llama_sample_token_greedy(ctx, new IntPtr(&st));
        }

        /// <summary>
        /// Mirostat 1.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `LLamaTokenData` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="m">The number of tokens considered in the estimation of `s_hat`. This is an arbitrary value that is used to calculate `s_hat`, which in turn helps to calculate the value of `k`. In the paper, they use `m = 100`, but you can experiment with different values to see how it affects the performance of the algorithm.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        public static int TokenMirostat(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, float tau, float eta, int m, ref float mu)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            int res;
            fixed (float* pmu = &mu)
            {
                res = NativeApi.llama_sample_token_mirostat(ctx, new IntPtr(&st), tau, eta, m, pmu);
            }

            return res;
        }

        /// <summary>
        /// Mirostat 2.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `LLamaTokenData` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        public static int TokenMirostatV2(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, float tau, float eta, ref float mu)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            int res;
            fixed (float* pmu = &mu)
            {
                res = NativeApi.llama_sample_token_mirostat_v2(ctx, new IntPtr(&st), tau, eta, pmu);
            }

            return res;
        }

        /// <summary>
        /// Top-K sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="k"></param>
        /// <param name="min_keep"></param>
        public static void TopK(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, int k, ulong min_keep)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            NativeApi.llama_sample_top_k(ctx, new IntPtr(&st), k, min_keep);
        }

        /// <summary>
        /// Nucleus sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        public static void TopP(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, float p, ulong min_keep)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            NativeApi.llama_sample_top_p(ctx, new IntPtr(&st), p, min_keep);
        }

        /// <summary>
        /// Locally Typical Sampling implementation described in the paper https://arxiv.org/abs/2202.00666.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        public static void Typical(SafeLLamaContextHandle ctx, LlamaTokenDataArray candidates, float p, ulong min_keep)
        {
            System.Buffers.MemoryHandle handle = candidates.data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.size,
                sorted = candidates.sorted
            };
            NativeApi.llama_sample_typical(ctx, new IntPtr(&st), p, min_keep);
        }
    }
}