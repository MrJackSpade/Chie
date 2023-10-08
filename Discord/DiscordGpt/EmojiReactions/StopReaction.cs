using Discord;
using DiscordGpt.Constants;
using DiscordGpt.Interfaces;

namespace DiscordGpt.EmojiReactions
{
	public class StopReaction : IReactionAction
	{
		public bool AllowBot => false;

		public string EmojiName => Emojis.STOP;

		public Task OnReactionAdded(IUser addedUser, IUserMessage message, int newCount) => Task.CompletedTask;

		public Task OnReactionRemoved(IUser addedUser, IUserMessage message, int newCount) => Task.CompletedTask;
	}
}