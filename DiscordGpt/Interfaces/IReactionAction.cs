using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordGpt.Interfaces
{
    public interface IReactionAction
    {
        string EmojiName { get; }
        bool AllowBot { get; }

        Task OnReactionAdded(IUser addedUser, IUserMessage message, int newCount);
        Task OnReactionRemoved(IUser addedUser, IUserMessage message, int newCount);

    }
}
