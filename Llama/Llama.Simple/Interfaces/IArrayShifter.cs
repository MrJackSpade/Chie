using Llama.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Simple.Interfaces
{
    internal interface IArrayShifter<T>
    {
        void Decode(BatchDecode<T> llamaBatch);
        void RemoveCacheTokens(uint clearStart, uint clearEnd);
        void ShiftCacheTokens(uint v1, uint start, uint v2, int amount);
        void Validate(KvCacheState<T> kvCache);
    }
}
