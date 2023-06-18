using Discord;
using DiscordGpt.Constants;

namespace DiscordGpt.Extensions
{
    public static class IMessageExtensions
    {
        public static bool IsVisible(this IMessage message)
        {
            if (message.Content.Contains("[Hide]", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (message.Content.Contains(Emojis.NINJA))
            {
                return false;
            }

            return true;
        }
    }
}