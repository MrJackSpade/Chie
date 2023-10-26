using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Data.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class LlamaBatch
    {
        public int NTokens { get; set; }
        public int[] Tokens { get; set; }
        public float[] Embds { get; set; }
        public int[] Pos { get; set; }
        public int[] NSeqId { get; set; }
        public int[][] SeqId { get; set; }
        public byte[] Logits { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
