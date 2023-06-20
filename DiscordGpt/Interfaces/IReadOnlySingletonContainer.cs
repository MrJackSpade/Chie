using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordGpt.Interfaces
{
    public interface IReadOnlySingletonContainer<T>
    {
        T? Value { get; }
    }
}
