using Discord;

namespace DiscordGpt.Interfaces
{
	public interface IReactionAction
	{
		bool AllowBot { get; }

		string EmojiName { get; }

		Task OnReactionAdded(IUser addedUser, IUserMessage message, int newCount);

		Task OnReactionRemoved(IUser addedUser, IUserMessage message, int newCount);
	}
}