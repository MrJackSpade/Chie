using Llama.Data.Native;
using System.Runtime.InteropServices;

namespace Llama.Native
{
    internal unsafe partial class LlamaCppApi
    {
#if WINDOWS

        private const string LIBRARY_NAME = "llama";

#else
        private const string LIBRARY_NAME = "libllama.so";
#endif

        static LlamaCppApi()
        {
            InitBackend(false);
        }

        /// <summary>
        /// Apply a LoRA adapter to a loaded model
        /// path_base_model is the path to a higher quality model to use as a base for
        /// the layers modified by the adapter. Can be NULL to use the current loaded model.
        /// The model needs to be reloaded before applying a new adapter, otherwise the adapter
        /// will be applied on top of the previous one
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="path_lora"></param>
        /// <param name="path_base_model"></param>
        /// <param name="n_threads"></param>
        /// <returns>Returns 0 on success</returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_apply_lora_from_file")]
        public static extern int ApplyLoraFromFile(SafeLlamaContextHandle ctx, string path_lora, string path_base_model, int n_threads);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_context_default_params")]
        public static extern LlamaContextParams ContextDefaultParams();

        /// <summary>
        /// Copy all tokens that belong to the specified source sequence to another destination sequence.
        /// Note that this does not allocate extra KV cache memory - it simply assigns the tokens to the new sequence.
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_kv_cache_seq_cp")]
        public static extern void CopyCacheTokens(SafeLlamaContextHandle handle, int sourceSequenceId, int destinationSequenceId, int startPos, int endPos);

        /// <summary>
        /// Copies the state to the specified destination address.
        /// Destination needs to have allocated enough memory.
        /// Returns the number of bytes copied
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_copy_state_data")]
        public static extern ulong CopyStateData(SafeLlamaContextHandle ctx, byte[] dest);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_decode")]
        public static extern int Decode(SafeLlamaContextHandle ctx, LlamaBatchNative batch);

        /// <summary>
        /// Run the llama inference to obtain the logits and probabilities for the next token.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tokens">The new tokens to process</param>
        /// <param name="n_tokens">The number of new tokens to process</param>
        /// <param name="n_past">The number of tokens to use from previous eval calls</param>
        /// <param name="n_threads"></param>
        /// <returns>Returns 0 on success</returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_eval")]
        public static extern int Eval(SafeLlamaContextHandle ctx, int[] tokens, int n_tokens, int n_past, int n_threads);

        /// <summary>
        /// Frees all allocated memory
        /// </summary>
        /// <param name="ctx"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_free")]
        public static extern void FreeContext(IntPtr ctx);

        /// <summary>
        /// Frees all allocated memory
        /// </summary>
        /// <param name="ctx"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_free_model")]
        public static extern void FreeModel(IntPtr ctx);

        /// <summary>
        /// Returns the number of tokens in the KV cache
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_get_kv_cache_token_count")]
        public static extern int GetCacheTokenCount(SafeLlamaContextHandle ctx);

        /// <summary>
        /// Get the embeddings for the input
        /// shape: [n_embd] (1-dimensional)
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_get_embeddings")]
        public static extern float* GetEmbeddings(SafeLlamaContextHandle ctx);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_get_kv_cache")]
        public static extern IntPtr GetKvCache(SafeLlamaContextHandle ctx);

        /// <summary>
        /// Token logits obtained from the last call to llama_eval()
        /// The logits for the last token are stored in the last row
        /// Can be mutated in order to change the probabilities of the next token
        /// Rows: n_tokens
        /// Cols: n_vocab
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_get_logits")]
        public static extern float* GetLogits(SafeLlamaContextHandle ctx);

        /// <summary>
        /// Returns the maximum size in bytes of the state (rng, logits, embedding
        /// and kv_cache) - will often be smaller after compacting tokens
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_get_state_size")]
        public static extern ulong GetStateSize(SafeLlamaContextHandle ctx);

        /// <summary>
        /// not great API - very likely to change.
        /// Initialize the llama + ggml backend
        /// Call once at the start of the program
        /// </summary>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_backend_init")]
        public static extern void InitBackend(bool numa);

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// Return NULL on failure
        /// </summary>
        /// <param name="path_model"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_init_from_file")]
        public static extern IntPtr InitFromFile(string path_model, LlamaContextParams params_);

        /// <summary>
        /// Removes all tokens that do not belong to the specified sequence.
        /// </summary>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_kv_cache_seq_keep")]
        public static extern void KeepCacheTokens(SafeLlamaContextHandle handle, int sequenceId);

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// Return NULL on failure
        /// </summary>
        /// <param name="path_model"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_load_model_from_file")]
        public static extern IntPtr LoadModelFromFile(string path_model, LlamaModelParams params_);

        /// <summary>
        /// Load session file
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="path_session"></param>
        /// <param name="tokens_out"></param>
        /// <param name="n_token_capacity"></param>
        /// <param name="n_token_count_out"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_load_session_file")]
        public static extern bool LoadSessionFile(SafeLlamaContextHandle ctx, string path_session, int[] tokens_out, ulong n_token_capacity, ulong* n_token_count_out);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_mlock_supported")]
        public static extern bool MlockSupported();

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_mmap_supported")]
        public static extern bool MmapSupported();

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_model_default_params")]
        public static extern LlamaModelParams ModelDefaultParams();

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_n_ctx")]
        public static extern int NCtx(SafeLlamaContextHandle ctx);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_n_embd")]
        public static extern int NEmbd(SafeLlamaContextHandle ctx);

        /// <summary>
        /// Various functions for loading a ggml llama model.
        /// Allocate (almost) all memory needed for the model.
        /// Return NULL on failure
        /// </summary>
        /// <param name="path_model"></param>
        /// <param name="params_"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_new_context_with_model")]
        public static extern IntPtr NewContextWithModel(SafeLlamaModelHandle mdl, LlamaContextParams params_);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_n_vocab")]
        public static extern int NVocab(SafeLlamaModelHandle ctx);

        /// <summary>
        /// Print system information
        /// </summary>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_print_system_info")]
        public static extern IntPtr PrintSystemInfo();

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_print_timings")]
        public static extern void PrintTimings(SafeLlamaContextHandle ctx);

        /// <summary>
        /// Removes all tokens that belong to the specified sequence and have positions in [startPos, endPos)
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_kv_cache_seq_rm")]
        public static extern void RemoveCacheTokens(SafeLlamaContextHandle handle, int sequenceId, int startPos, int endPos);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_reset_timings")]
        public static extern void ResetTimings(SafeLlamaContextHandle ctx);

        /// <summary>
        /// Save session file
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="path_session"></param>
        /// <param name="tokens"></param>
        /// <param name="n_token_count"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_save_session_file")]
        public static extern bool SaveSessionFile(SafeLlamaContextHandle ctx, string path_session, int[] tokens, ulong n_token_count);

        /// <summary>
        /// Sets the current rng seed.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="seed"></param>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_set_rng_seed")]
        public static extern void SetRngSeed(SafeLlamaContextHandle ctx, int seed);

        /// <summary>
        /// Set the state reading from the specified address
        /// Returns the number of bytes read
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_set_state_data")]
        public static extern ulong SetStateData(SafeLlamaContextHandle ctx, byte[] src);

        /// <summary>
        /// Adds relative position "delta" to all tokens that belong to the specified sequence and have positions in [startPos, endPos)
        /// If the KV cache is RoPEd, the KV data is updated accordingly.
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_kv_cache_seq_add")]
        public static extern void ShiftCacheTokens(SafeLlamaContextHandle handle, int sequenceId, int startPos, int endPos, int delta);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_token_bos")]
        public static extern int TokenBos();

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_token_eos")]
        public static extern int TokenEos();

        /// <summary>
        /// Convert the provided text into tokens.
        /// The tokens pointer must be large enough to hold the resulting tokens.
        /// Returns the number of tokens on success, no more than n_max_tokens
        /// Returns a negative number on failure - the number of tokens that would have been returned
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="text"></param>
        /// <param name="tokens"></param>
        /// <param name="n_max_tokens"></param>
        /// <param name="add_bos"></param>
        /// <returns></returns>
        public static int Tokenize(SafeLlamaModelHandle ctx, string text, int[] tokens, int n_max_tokens, bool add_bos, bool special = false)
        {
            return TokenizeNative(ctx, text, text.Length, tokens, n_max_tokens, add_bos, special);
        }

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_tokenize")]
        public static extern int TokenizeNative(SafeLlamaModelHandle model, [MarshalAs(UnmanagedType.LPStr)] string text, int textLen, [Out] int[] tokens, int maxTokens, bool addBos, bool special);

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_token_nl")]
        public static extern int TokenNl();

        [DllImport(LIBRARY_NAME, EntryPoint = "llama_token_to_piece", CharSet = CharSet.Ansi)]
        public static extern int TokenToPiece(SafeLlamaModelHandle model, int token, [Out] char[] buf, int length);

        /// <summary>
        /// Token Id -> String. Uses the vocabulary in the provided context
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns>Pointer to a string.</returns>
        [DllImport(LIBRARY_NAME, EntryPoint = "llama_token_get_text")]
        public static extern IntPtr TokenToStr(SafeLlamaContextHandle ctx, int token);
    }
}