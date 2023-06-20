using Discord.WebSocket;
using DiscordGpt.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DiscordGpt.Interfaces
{
    public interface IActiveMessageContainer : ISingletonContainer<ActiveMessage>
    {
        Task Create(ISocketMessageChannel channel, long messageId);

        Task Finalize(string content);
    }
}
