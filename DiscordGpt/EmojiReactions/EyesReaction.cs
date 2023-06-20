using Discord;
using DiscordGpt.Constants;
using DiscordGpt.Interfaces;
using DiscordGpt.Models;

namespace DiscordGpt.EmojiReactions
{
    public class EyesReaction : IReactionAction
    {
        private readonly IReadOnlySingletonContainer<ActiveMessage> _container;

        public EyesReaction(IReadOnlySingletonContainer<ActiveMessage> activeMessageContainer)
        {
            this._container = activeMessageContainer;
        }

        public bool AllowBot => false;

        public string EmojiName => Emojis.EYES;

        public async Task OnReactionAdded(IUser addedUser, IUserMessage message, int newCount)
        {
            if (this._container.Value is null)
            {
                return;
            }

            if(message.Id != this._container.Value.RestUserMessage.Id)
            {
                return;
            }

            await this._container.Value.SetVisible(newCount > 1);
        }

        public async Task OnReactionRemoved(IUser addedUser, IUserMessage message, int newCount)
        {
            if (this._container.Value is null)
            {
                return;
            }

            if (message.Id != this._container.Value.RestUserMessage.Id)
            {
                return;
            }

            await this._container.Value.SetVisible(newCount > 1);
        }
    }
}