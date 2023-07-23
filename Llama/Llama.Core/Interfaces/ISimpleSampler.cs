using Llama.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Core.Interfaces
{
    public interface ISimpleSampler
    {
        public void SampleNext(SampleContext context);
    }
}