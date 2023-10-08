using Discord;
using DiscordGpt.Interfaces;
using System.Diagnostics;

namespace DiscordGpt.EmojiReactions
{
	public class ReactionActionCollection
	{
		private readonly List<IReactionAction> _actions = new();

		private string? _messageOwner;

		public ReactionActionCollection(IEnumerable<IReactionAction> actions)
		{
			this._actions = actions.ToList();
		}

		public async Task AddReaction(string reactionName, int remaining, IUser addedUser, IUserMessage message)
		{
			if (string.IsNullOrWhiteSpace(this._messageOwner))
			{
				throw new ArgumentException("Message owner must be set");
			}

			if (message.Author.Username != this._messageOwner)
			{
				return;
			}

			foreach (IReactionAction action in this._actions)
			{
				if (action.EmojiName != reactionName)
				{
					continue;
				}

				if (!action.AllowBot && (addedUser?.IsBot ?? true))
				{
					return;
				}

				await action.OnReactionAdded(addedUser, message, remaining);
			}
		}

		public async Task RemoveReaction(string reactionName, int remaining, IUser addedUser, IUserMessage message)
		{
			if (string.IsNullOrWhiteSpace(this._messageOwner))
			{
				throw new ArgumentException("Message owner must be set");
			}

			if (message.Author.Username != this._messageOwner)
			{
				return;
			}

			foreach (IReactionAction action in this._actions)
			{
				if (action.EmojiName != reactionName)
				{
					continue;
				}

				if (!action.AllowBot && (addedUser?.IsBot ?? true))
				{
					return;
				}

				try
				{
					await action.OnReactionRemoved(addedUser, message, remaining);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}
		}

		public void SetOwner(string messageOwner)
		{
			if (string.IsNullOrWhiteSpace(messageOwner))
			{
				throw new ArgumentException($"'{nameof(messageOwner)}' cannot be null or whitespace.", nameof(messageOwner));
			}

			if (this._messageOwner != null)
			{
				throw new Exception("Message owner already set");
			}

			this._messageOwner = messageOwner;
		}
	}
}