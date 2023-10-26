using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Data.Native
{
    /// <summary>
    /// Represents a batch of data for llama.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaBatchNative
    {
        /// <summary>
        /// Number of tokens.
        /// </summary>
        public int NTokens;

        /// <summary>
        /// Pointer to the tokens array.
        /// </summary>
        public IntPtr Token;

        /// <summary>
        /// Pointer to the embeddings array.
        /// </summary>
        public IntPtr Embd;

        /// <summary>
        /// Pointer to the positions array.
        /// </summary>
        public IntPtr Pos;

        /// <summary>
        /// Pointer to the sequence IDs count array.
        /// </summary>
        public IntPtr NSeqId;

        /// <summary>
        /// Pointer to the sequence IDs array.
        /// </summary>
        public IntPtr SeqId;

        /// <summary>
        /// Pointer to the logits array.
        /// </summary>
        public IntPtr Logits;

        /// <summary>
        /// Used if pos is NULL. See struct comments for more details.
        /// </summary>
        public int AllPos0;

        /// <summary>
        /// Used if pos is NULL. See struct comments for more details.
        /// </summary>
        public int AllPos1;

        /// <summary>
        /// Used if seq_id is NULL. See struct comments for more details.
        /// </summary>
        public int AllSeqId;
    }
}
