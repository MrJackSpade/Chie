using Discord;
using DiscordGpt.Constants;
using DiscordGpt.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordGpt.EmojiReactions
{
    public class StopReaction : IReactionAction
    {
        public string EmojiName => Emojis.STOP;

        public bool AllowBot => false;

        public Task OnReactionAdded(IUser addedUser, IUserMessage message, int newCount) => Task.CompletedTask;
        public Task OnReactionRemoved(IUser addedUser, IUserMessage message, int newCount) => Task.CompletedTask;
    }
}
