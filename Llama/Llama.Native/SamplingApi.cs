using Llama.Data.Native;

namespace Llama.Native
{
    public unsafe class SamplingApi
    {
        public static bool ContainsNonEnglishCharacters(string input)
        {
            // Iterate through each character in the string
            foreach (char c in input)
            {
                // Check if the character is outside the basic Latin and Latin-1 Supplement range
                if (c is (< '\u0000' or > '\u007F') and (< '\u00A0' or > '\u00FF'))
                {
                    // If the character is outside these ranges, it's a non-English character
                    return true;
                }
            }

            // If no non-English characters were found, return false
            return false;
        }

        public static void MinP(LlamaTokenDataArray candidates, float min)
        {
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                LlamaTokenData data = candidates.Data.Span[i];

                if (data.p < min)
                {
                    candidates.Data.Span[i].logit = float.NegativeInfinity;
                }
            }

            candidates.Sorted = false;
        }

        public static void MinP(LlamaTokenDataArray candidates, int tokenId, float min)
        {
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                LlamaTokenData data = candidates.Data.Span[i];

                if (data.id == tokenId)
                {
                    if (data.p < min)
                    {
                        candidates.Data.Span[i].logit = float.NegativeInfinity;
                        candidates.Sorted = false;
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Frequency and presence penalties described in OpenAI API https://platform.openai.com/docs/api-reference/parameter-details.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="check_tokens"></param>
        /// <param name="last_tokens_size"></param>
        /// <param name="alpha_frequency"></param>
        /// <param name="alpha_presence"></param>
        public static void RepetitionPenalties(LlamaTokenDataArray candidates, int[] lastTokens, float penaltyRepeat, float penaltyFreq, float penaltyPresent, float slopeRepeat = 0)
        {
            // Early return condition
            if (lastTokens.Length == 0 || (penaltyRepeat == 1.0f && penaltyFreq == 0.0f && penaltyPresent == 0.0f))
            {
                return;
            }

            // Create a frequency map
            Dictionary<int, FoundTokenData> tokenCount = new();
            for (int i = 0; i < lastTokens.Length; i++)
            {
                if (!tokenCount.TryGetValue(lastTokens[i], out FoundTokenData ftd))
                {
                    ftd = new FoundTokenData();
                    tokenCount[lastTokens[i]] = ftd;
                }

                ftd.Count++;
                ftd.LastIndex = i;
            }

            // Apply penalties
            for (int i = 0; i < (int)candidates.Size; i++)
            {
                if (!tokenCount.TryGetValue(candidates.Data.Span[i].id, out FoundTokenData ftd))
                {
                    continue;
                }

                if (penaltyRepeat > 0)
                {
                    float adjPenalty = CalculateAdjustedPenalty(penaltyRepeat, slopeRepeat, ftd.LastIndex, lastTokens.Length);

                    // Applying penalties
                    if (candidates.Data.Span[i].logit <= 0)
                    {
                        candidates.Data.Span[i].logit *= adjPenalty;
                    }
                    else
                    {
                        candidates.Data.Span[i].logit /= adjPenalty;
                    }
                }

                candidates.Data.Span[i].logit -= ftd.Count * penaltyFreq + (ftd.Count > 0 ? 1f : 0f) * penaltyPresent;
            }

            candidates.Sorted = false;
        }

        /// <summary>
        /// Sorts candidate tokens by their logits in descending order and calculate probabilities based on logits.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        public static void SoftMax(LlamaTokenDataArray candidates)
        {
            if (candidates.Size <= 0)
            {
                throw new InvalidOperationException("Candidates array cannot be empty.");
            }

            var candidateSpan = candidates.Data.Span;

            // Sort the logits in descending order
            if (!candidates.Sorted)
            {
                // Using LINQ to sort, then copy back to the MemorySpan
                var sortedData = candidates.Data.ToArray();

                Array.Sort(sortedData, (a, b) => b.logit.CompareTo(a.logit));

                for (int i = 0; i < sortedData.Length; i++)
                {
                    candidateSpan[i] = sortedData[i];
                }

                candidates.Sorted = true;
            }

            float maxLogit = candidateSpan[0].logit;

            float cumSum = 0.0f;

            // Compute exponential values and cumulate sum
            for (int i = 0; i < candidateSpan.Length; i++)
            {
                float expValue = (float)Math.Exp(candidateSpan[i].logit - maxLogit);

                candidateSpan[i].p = expValue;

                if (float.IsNaN(expValue))
                {
                    throw new ArgumentOutOfRangeException();
                }

                cumSum += expValue;

                if (float.IsNaN(cumSum))
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            int index = 0;
            // Normalize probabilities
            for (int i = 0; i < candidateSpan.Length; i++)
            {
                LlamaTokenData data = candidateSpan[i];

                candidateSpan[i].p /= cumSum;

                if (float.IsNaN(candidateSpan[i].p))
                {
                    throw new ArgumentOutOfRangeException();
                }

                index++;
            }
        }

        public static void SurpressNewline(SafeLlamaModelHandle handle, LlamaTokenDataArray candidates)
        {
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                LlamaTokenData token = candidates.Data.Span[i];

                string value = NativeApi.TokenToPiece(handle, token.id);

                if (value.Contains('\n') && value != "\n")
                {
                    candidates.Data.Span[i].logit = float.NegativeInfinity;
                }
            }

            candidates.Sorted = false;
        }

        public static void SurpressNonEnglish(SafeLlamaModelHandle handle, LlamaTokenDataArray candidates)
        {
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                LlamaTokenData token = candidates.Data.Span[i];

                string value = NativeApi.TokenToPiece(handle, token.id);

                if (ContainsNonEnglishCharacters(value))
                {
                    candidates.Data.Span[i].logit = float.NegativeInfinity;
                }
            }

            candidates.Sorted = false;
        }

        /// <summary>
        /// Tail Free Sampling described in https://www.trentonbricken.com/Tail-Free-Sampling/.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="z"></param>
        /// <param name="min_keep"></param>
        public static void TailFree(LlamaTokenDataArray candidates, float z, int minKeep)
        {
            if (z >= 1.0f || candidates.Size <= 2)
            {
                return;
            }

            SoftMax(candidates); // Assuming LlamaSampleSoftmax is defined elsewhere

            var firstDerivatives = new List<float>();
            for (int i = 0; i < (int)(candidates.Size - 1); i++)
            {
                firstDerivatives.Add(candidates.Data.Span[i].p - candidates.Data.Span[i + 1].p);
            }

            var secondDerivatives = new List<float>();
            for (int i = 0; i < firstDerivatives.Count - 1; i++)
            {
                secondDerivatives.Add(firstDerivatives[i] - firstDerivatives[i + 1]);
            }

            for (int i = 0; i < secondDerivatives.Count; i++)
            {
                secondDerivatives[i] = Math.Abs(secondDerivatives[i]);
            }

            NormalizeSecondDerivatives(secondDerivatives);

            float cumSum = 0.0f;
            int lastIdx = (int)candidates.Size;
            for (int i = 0; i < secondDerivatives.Count; i++)
            {
                cumSum += secondDerivatives[i];
                if (cumSum > z && i >= minKeep)
                {
                    lastIdx = i + 1; // Adjusted to C# indexing
                    break;
                }
            }

            candidates.Size = (ulong)lastIdx;

            for (int i = lastIdx + 1; i < candidates.Data.Span.Length; i++)
            {
                candidates.Data.Span[i].logit = float.NegativeInfinity;
            }

            candidates.Sorted = false;
        }

        public static void Temperature(LlamaTokenDataArray candidates, float temp)
        {
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                candidates.Data.Span[i].logit = candidates.Data.Span[i].logit / temp;
            }

            candidates.Sorted = false;
        }

        public static void Temperature(LlamaTokenDataArray candidates, int index, float temp)
        {
            candidates.Data.Span[index].logit = candidates.Data.Span[index].logit / temp;

            candidates.Sorted = false;
        }

        /// <summary>
        /// Randomly selects a token from the candidates based on their probabilities.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <returns></returns>
        public static int Token(SafeLlamaContextHandle ctx, LlamaTokenDataArray candidates)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Sorted
            };

            return LlamaCppApi.SampleToken(ctx, new IntPtr(&st));
        }

        /// <summary>
        /// Selects the token with the highest probability.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <returns></returns>
        public static int TokenGreedy(LlamaTokenDataArray candidates)
        {
            SoftMax(candidates);
            return candidates.Data.Span[0].id;
        }

        /// <summary>
        /// Mirostat 1.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `LlamaTokenData` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="m">The number of tokens considered in the estimation of `s_hat`. This is an arbitrary value that is used to calculate `s_hat`, which in turn helps to calculate the value of `k`. In the paper, they use `m = 100`, but you can experiment with different values to see how it affects the performance of the algorithm.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        public static int TokenMirostat(SafeLlamaContextHandle ctx, LlamaTokenDataArray candidates, float tau, float eta, int m, ref float mu)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Sorted
            };
            int res;
            fixed (float* pmu = &mu)
            {
                res = LlamaCppApi.SampleTokenMirostat(ctx, new IntPtr(&st), tau, eta, m, pmu);
            }

            return res;
        }

        /// <summary>
        /// Mirostat 2.0 algorithm described in the paper https://arxiv.org/abs/2007.14966. Uses tokens instead of words.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">A vector of `LlamaTokenData` containing the candidate tokens, their probabilities (p), and log-odds (logit) for the current position in the generated text.</param>
        /// <param name="tau">The target cross-entropy (or surprise) value you want to achieve for the generated text. A higher value corresponds to more surprising or less predictable text, while a lower value corresponds to less surprising or more predictable text.</param>
        /// <param name="eta">The learning rate used to update `mu` based on the error between the target and observed surprisal of the sampled word. A larger learning rate will cause `mu` to be updated more quickly, while a smaller learning rate will result in slower updates.</param>
        /// <param name="mu">Maximum cross-entropy. This value is initialized to be twice the target cross-entropy (`2 * tau`) and is updated in the algorithm based on the error between the target and observed surprisal.</param>
        /// <returns></returns>
        public static int TokenMirostatV2(SafeLlamaContextHandle ctx, LlamaTokenDataArray candidates, float tau, float eta, ref float mu)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Sorted
            };
            int res;
            fixed (float* pmu = &mu)
            {
                res = LlamaCppApi.SampleTokenMirostatV2(ctx, new IntPtr(&st), tau, eta, pmu);
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
        public static void TopK(LlamaTokenDataArray candidates, int k, int min_keep)
        {
            SoftMax(candidates);

            for (int i = Math.Max(k, min_keep); i < candidates.Data.Span.Length; i++)
            {
                candidates.Data.Span[i].logit = float.NegativeInfinity;
            }

            candidates.Size = (ulong)Math.Max(k, min_keep);
        }

        /// <summary>
        /// Nucleus sampling described in academic paper "The Curious Case of Neural Text Degeneration" https://arxiv.org/abs/1904.09751
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        public static void TopP(SafeLlamaContextHandle ctx, LlamaTokenDataArray candidates, float p, ulong min_keep)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Sorted
            };

            LlamaCppApi.SampleTopP(ctx, new IntPtr(&st), p, min_keep);

            candidates.Size = st.size;
            candidates.Sorted = st.sorted;
        }

        /// <summary>
        /// Locally Typical Sampling implementation described in the paper https://arxiv.org/abs/2202.00666.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="candidates">Pointer to LlamaTokenDataArray</param>
        /// <param name="p"></param>
        /// <param name="min_keep"></param>
        public static void Typical(SafeLlamaContextHandle ctx, LlamaTokenDataArray candidates, float p, ulong min_keep)
        {
            System.Buffers.MemoryHandle handle = candidates.Data.Pin();
            LlamaTokenDataArrayNative st = new()
            {
                data = new IntPtr(handle.Pointer),
                size = candidates.Size,
                sorted = candidates.Sorted
            };

            LlamaCppApi.SampleTypical(ctx, new IntPtr(&st), p, min_keep);

            candidates.Size = st.size;
            candidates.Sorted = st.sorted;
        }

        // Assumptions:
        // ftd.LastIndex is a value that decreases as we get closer to the token we want to apply the penalty to.
        // lastTokens.Length is the total number of tokens we're considering.
        // penaltyRepeat is the original penalty value.
        // slope controls the rate of change of the penalty.
        private static float CalculateAdjustedPenalty(float penaltyRepeat, float slope, int ftdLastIndex, int lastTokensLength)
        {
            if (slope == 0)
            {
                // When slope is 0, penaltyRepeat remains unchanged
                return penaltyRepeat;
            }
            else
            {
                // Calculate the normalized position of ftd.LastIndex in the range [0, lastTokens.Length]
                float normalizedIndex = (float)ftdLastIndex / lastTokensLength;

                // Adjust the penaltyRepeat to approach 1 as ftd.LastIndex approaches 0
                // This creates a linear interpolation between penaltyRepeat and 1, controlled by slope
                float adjustedPenalty = ((1 - normalizedIndex) * slope * (1 - penaltyRepeat)) + penaltyRepeat;

                return adjustedPenalty;
            }
        }

        private static void NormalizeSecondDerivatives(List<float> secondDerivatives)
        {
            float sum = secondDerivatives.Sum();
            if (sum > 1e-6f)
            {
                for (int i = 0; i < secondDerivatives.Count; i++)
                {
                    secondDerivatives[i] /= sum;
                }
            }
            else
            {
                float equalValue = 1.0f / secondDerivatives.Count;
                for (int i = 0; i < secondDerivatives.Count; i++)
                {
                    secondDerivatives[i] = equalValue;
                }
            }
        }

        private class FoundTokenData
        {
            public int Count { get; set; } = 0;

            public int LastIndex { get; set; } = 0;
        }
    }
}