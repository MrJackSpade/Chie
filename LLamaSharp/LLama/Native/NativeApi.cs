using Llama.Exceptions;
using System;
using System.Runtime.InteropServices;
using System.Text;
using llama_token_id = System.Int32;

namespace Llama.Native
{
    public unsafe partial class NativeApi
	{
		private const string LIBRARY_NAME = "llama";

		static NativeApi()
		{
			try
			{
				llama_empty_call();
			}
			catch (DllNotFoundException)
			{
				throw new RuntimeError("The native library cannot be found. It could be one of the following reasons: \n" +
					"1. No LLamaSharp backend was installed. Please search LLamaSharp.Backend and install one of them. \n" +
					"2. You are using a device with only CPU but installed cuda backend. Please install cpu backend instead. \n" +
					"3. The backend is not compatible with your system cuda environment. Please check and fix it. If the environment is " +
					"expected not to be changed, then consider build llama.cpp from source or submit an issue to LLamaSharp.");
			}

			NativeApi.llama_init_backend();
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
		[DllImport(LIBRARY_NAME)]
		public static extern int llama_apply_lora_from_file(SafeLlamaContext ctx, string path_lora, string path_base_model, int n_threads);

		[DllImport(LIBRARY_NAME)]
		public static extern LlamaContextParams llama_context_default_params();

		/// <summary>
		/// Copies the state to the specified destination address.
		/// Destination needs to have allocated enough memory.
		/// Returns the number of bytes copied
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="dest"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern ulong llama_copy_state_data(SafeLlamaContext ctx, byte[] dest);

		[DllImport(LIBRARY_NAME, EntryPoint = "llama_mmap_supported")]
		public static extern bool llama_empty_call();

		/// <summary>
		/// Run the llama inference to obtain the logits and probabilities for the next token.
		/// tokens + n_tokens is the provided batch of new tokens to process
		/// n_past is the number of tokens to use from previous eval calls
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="tokens"></param>
		/// <param name="n_tokens"></param>
		/// <param name="n_past"></param>
		/// <param name="n_threads"></param>
		/// <returns>Returns 0 on success</returns>
		[DllImport(LIBRARY_NAME)]
		public static extern int llama_eval(SafeLlamaContext ctx, llama_token_id[] tokens, int n_tokens, int n_past, int n_threads);

		/// <summary>
		/// Frees all allocated memory
		/// </summary>
		/// <param name="ctx"></param>
		[DllImport(LIBRARY_NAME)]
		public static extern void llama_free(IntPtr ctx);

		/// <summary>
		/// Get the embeddings for the input
		/// shape: [n_embd] (1-dimensional)
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern float* llama_get_embeddings(SafeLlamaContext ctx);

		/// <summary>
		/// Returns the number of tokens in the KV cache
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern int llama_get_kv_cache_token_count(SafeLlamaContext ctx);

		/// <summary>
		/// Token logits obtained from the last call to llama_eval()
		/// The logits for the last token are stored in the last row
		/// Can be mutated in order to change the probabilities of the next token
		/// Rows: n_tokens
		/// Cols: n_vocab
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern float* llama_get_logits(SafeLlamaContext ctx);

		/// <summary>
		/// Returns the maximum size in bytes of the state (rng, logits, embedding
		/// and kv_cache) - will often be smaller after compacting tokens
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern ulong llama_get_state_size(SafeLlamaContext ctx);

		/// <summary>
		/// not great API - very likely to change.
		/// Initialize the llama + ggml backend
		/// Call once at the start of the program
		/// </summary>
		[DllImport(LIBRARY_NAME)]
		public static extern void llama_init_backend();

		/// <summary>
		/// Various functions for loading a ggml llama model.
		/// Allocate (almost) all memory needed for the model.
		/// Return NULL on failure
		/// </summary>
		/// <param name="path_model"></param>
		/// <param name="params_"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern IntPtr llama_init_from_file(string path_model, LlamaContextParams params_);

		/// <summary>
		/// Load session file
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="path_session"></param>
		/// <param name="tokens_out"></param>
		/// <param name="n_token_capacity"></param>
		/// <param name="n_token_count_out"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern bool llama_load_session_file(SafeLlamaContext ctx, string path_session, llama_token_id[] tokens_out, ulong n_token_capacity, ulong* n_token_count_out);

		[DllImport(LIBRARY_NAME)]
		public static extern bool llama_mlock_supported();

		[DllImport(LIBRARY_NAME)]
		public static extern bool llama_mmap_supported();

		[DllImport(LIBRARY_NAME)]
		public static extern int llama_n_ctx(SafeLlamaContext ctx);

		[DllImport(LIBRARY_NAME)]
		public static extern int llama_n_embd(SafeLlamaContext ctx);

		[DllImport(LIBRARY_NAME)]
		public static extern int llama_n_vocab(SafeLlamaContext ctx);

		/// <summary>
		/// Print system information
		/// </summary>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern IntPtr llama_print_system_info();

		[DllImport(LIBRARY_NAME)]
		public static extern void llama_print_timings(SafeLlamaContext ctx);

		[DllImport(LIBRARY_NAME)]
		public static extern void llama_reset_timings(SafeLlamaContext ctx);

		/// <summary>
		/// Save session file
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="path_session"></param>
		/// <param name="tokens"></param>
		/// <param name="n_token_count"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern bool llama_save_session_file(SafeLlamaContext ctx, string path_session, llama_token_id[] tokens, ulong n_token_count);

		/// <summary>
		/// Sets the current rng seed.
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="seed"></param>
		[DllImport(LIBRARY_NAME)]
		public static extern void llama_set_rng_seed(SafeLlamaContext ctx, int seed);

		/// <summary>
		/// Set the state reading from the specified address
		/// Returns the number of bytes read
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="src"></param>
		/// <returns></returns>
		[DllImport(LIBRARY_NAME)]
		public static extern ulong llama_set_state_data(SafeLlamaContext ctx, byte[] src);

		[DllImport(LIBRARY_NAME)]
		public static extern llama_token_id llama_token_bos();

		[DllImport(LIBRARY_NAME)]
		public static extern llama_token_id llama_token_eos();

		[DllImport(LIBRARY_NAME)]
		public static extern llama_token_id llama_token_nl();

		/// <summary>
		/// Token Id -> String. Uses the vocabulary in the provided context
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="token"></param>
		/// <returns>Pointer to a string.</returns>
		[DllImport(LIBRARY_NAME)]
		public static extern IntPtr llama_token_to_str(SafeLlamaContext ctx, llama_token_id token);

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
		public static int llama_tokenize(SafeLlamaContext ctx, string text, Encoding encoding, llama_token_id[] tokens, int n_max_tokens, bool add_bos)
		{
			byte[] bytes = encoding.GetBytes(text);
			sbyte[] data = new sbyte[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
			{
				data[i] = (sbyte)bytes[i];
				//if (bytes[i] < 128)
				//{
				//    data[i] = (sbyte)bytes[i];
				//}
				//else
				//{
				//    data[i] = (sbyte)(~((sbyte)(~bytes[i] + 1)) + 1);
				//}
			}

			return llama_tokenize_native(ctx, data, tokens, n_max_tokens, add_bos);
		}

		[DllImport(LIBRARY_NAME, EntryPoint = "llama_tokenize")]
		public static extern int llama_tokenize_native(SafeLlamaContext ctx, sbyte[] text, llama_token_id[] tokens, int n_max_tokens, bool add_bos);
	}
}