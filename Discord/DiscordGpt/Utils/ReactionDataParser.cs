using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace DiscordGpt.Utils
{
    public enum ReactionState
    {
        Added,

        Removed
    }

    public static class ReactionDataParser
    {
        public static async Task<ReactionData> GetDataAsync(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            IUserMessage reactedMessage = await cachedMessage.DownloadAsync();
            IUser user = null;

            if (reaction.User.IsSpecified)
            {
                user = reaction.User.Value;
            }
            else if (reactedMessage.Channel is RestDMChannel rd)
            {
                user = rd.Recipient;
            }

            int remaining = 0;

            if (reactedMessage.Reactions.TryGetValue(reaction.Emote, out ReactionMetadata metadata))
            {
                remaining = metadata.ReactionCount;
            }

            return new ReactionData()
            {
                Name = reaction.Emote.Name,
                ReactedMessage = reactedMessage,
                ReactedUser = user,
                RemainingCount = remaining
            };
        }
    }

    public class ReactionData
    {
        public bool IsSelf { get; set; }

        public string Name { get; set; }

        public IUserMessage ReactedMessage { get; set; }

        public IUser ReactedUser { get; set; }

        public int RemainingCount { get; set; }

        public ReactionState State { get; set; }
    }
}