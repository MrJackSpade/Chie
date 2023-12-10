using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Data.Models
{
    public class SpecialTokens
    {
        public int NewLine { get; set; } = 13;

        public int BOS { get; set; } = 1;

        public int EOS { get; set; } = 2;

        public int Null { get; set; } = -1;
    }
}
