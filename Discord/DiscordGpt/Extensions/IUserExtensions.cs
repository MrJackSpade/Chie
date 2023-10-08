using Discord;

namespace DiscordGpt.Extensions
{
	internal static class IUserExtensions
	{
		public static string GetDisplayName(this IUser user)
		{
			if (user is IGuildUser gu && !string.IsNullOrWhiteSpace(gu.Nickname))
			{
				return gu.Nickname;
			}

			return user.Username;
		}
	}
}