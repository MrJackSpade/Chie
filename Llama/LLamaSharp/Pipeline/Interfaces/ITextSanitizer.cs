using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Pipeline.Interfaces
{
    public interface ITextSanitizer
    {
        public string Sanitize(string text);
    }
}
