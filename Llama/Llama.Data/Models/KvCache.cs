using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Data.Models
{
    public class KvCache
    {
        public bool HasShift { get; set; }

        public uint Head { get; set; }

        public uint Size { get; set; }

        public uint N { get; set; }
    }
}
