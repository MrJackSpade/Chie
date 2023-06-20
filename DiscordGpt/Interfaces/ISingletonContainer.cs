using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordGpt.Interfaces
{
    public interface ISingletonContainer<T> : IReadOnlySingletonContainer<T>
    {
        void Clear();
        public void SetValue(T value);
    }
}
