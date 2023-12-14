using ChieApi.Shared.Interfaces;
using ChieApi.Shared.Models;
using Discord;
using DiscordGpt.Constants;
using DiscordGpt.Interfaces;
using DiscordGpt.Models;

namespace DiscordGpt.EmojiReactions
{
    public class ContinueReaction : IReactionAction
    {
        private readonly ActiveChannelCollection _activeChannels;

        private readonly IActiveMessageContainer _activeMessageContainer;

        private readonly IChieClient _chieClient;

        private bool? _startVisible;

        private Task<bool> StartVisible => this.GetStartVisible();

        private async Task<bool> GetStartVisible()
        {
            if (!_startVisible.HasValue)
            {
                _startVisible = (await _chieClient.StartVisible()).StartVisible;
            }

            return _startVisible.Value;
        }

        public ContinueReaction(IActiveMessageContainer activeMessageContainer, IChieClient chieClient, ActiveChannelCollection activeChannels)
        {
            this._activeChannels = activeChannels;
            this._chieClient = chieClient;
            this._activeMessageContainer = activeMessageContainer;
        }

        public bool AllowBot => false;

        public string EmojiName => Emojis.GO;

        public async Task OnReactionAdded(IUser addedUser, IUserMessage message, int newCount)
        {
            if (!this._activeChannels.TryGetValue(message.Channel.Id, out ActiveChannel? activeChannel))
            {
                return;
            }

            ContinueRequestResponse continueRequestResponse = await this._chieClient.ContinueRequest(activeChannel.ChieName);

            if (continueRequestResponse.Success)
            {
                await this._activeMessageContainer.Open(activeChannel.Channel, continueRequestResponse.MessageId, message.Id, await StartVisible);
            }
        }

        public Task OnReactionRemoved(IUser addedUser, IUserMessage message, int newCount) => Task.CompletedTask;
    }
}