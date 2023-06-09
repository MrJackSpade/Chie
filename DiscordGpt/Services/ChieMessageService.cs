using Ai.Utils;
using Ai.Utils.Extensions;
using ChieApi.Client;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Models;
using Discord;
using Discord.WebSocket;
using DiscordGpt.Constants;
using DiscordGpt.Extensions;
using System.Text.RegularExpressions;

namespace DiscordGpt.Services
{
	public class ChieMessageService
	{
		public List<QueuedMessage> _outgoingMessageQueue = new();

		private readonly ActiveChannelCollection _activeChannels;

		private readonly ChieClient _chieClient;

		private readonly Logger _logger;

		private readonly NameService _nameService;

		private readonly DelayedTrigger _outgoingMessageTrigger;

		private string _lastError = string.Empty;

		public ChieMessageService(ChieClient chieClient, Logger logger, ActiveChannelCollection activeChannels, NameService nameService)
		{
			this._activeChannels = activeChannels;
			this._nameService = nameService;
			this._logger = logger;
			this._chieClient = chieClient;
			this._outgoingMessageTrigger = new DelayedTrigger(this.Flush, 3000, 30_000);
		}

		public string CleanContent(string content)
		{
			string toReturn = content;

			//Remove custom emotes
			toReturn = Regex.Replace(toReturn, @"\<a?\:[a-zA-Z0-9\-]+\:\d+\>", "");

			return toReturn;
		}

		public async Task DeferredMessageProcessing(SocketMessage arg)
		{
			try
			{
				QueuedMessage[] queuedMessages = await this.GenerateQueuedMessages(arg).ToArray();

				if (!queuedMessages.Any())
				{
					await this._logger.Write("No valid messages...");
					return;
				}

				if (!this._outgoingMessageTrigger.TryFire(() => this._outgoingMessageQueue.AddRange(queuedMessages)))
				{
					foreach (QueuedMessage queuedMessage in queuedMessages)
					{
						await this.MarkUnseen(queuedMessage.SocketMessage);
					}
				}
			}
			catch (Exception ex)
			{
				await this._logger.Write("Exception occurred");
				await this._logger.Write(ex.ToString());
			}
		}

		public async Task<bool> Flush()
		{
			await this._logger.Write("Sending messages to client...");

			List<QueuedMessage> queuedMessages = this._outgoingMessageQueue.ToList();

			MessageSendResponse? sendResponse = null;

			try
			{
				sendResponse = await this._chieClient.Send(queuedMessages.Select(q => q.ChatEntry).ToArray());
				this._outgoingMessageQueue.Clear();
			}
			catch (Exception ex)
			{
				await this.LogIfDifferent(ex);
				return false;
			}

			if (!sendResponse.Success)
			{
				await this._logger.Write("Messages reported as unseen. Client busy?");

				foreach (SocketMessage socketMessage in queuedMessages.Select(m => m.SocketMessage).Distinct())
				{
					await this.MarkUnseen(socketMessage);
				}
			}

			return true;
		}

		public async Task MarkUnseen(SocketMessage message)
		{
			//TODO: Move to discord message service or use callback.
			//Socket messages are not relevant here
			Emoji ninja = Emoji.Parse(Emojis.NINJA);
			await message.AddReactionAsync(ninja);
		}

		public void TryDelaySend(DateTime newTarget) => this._outgoingMessageTrigger.ResetWait(newTarget);

		private async IAsyncEnumerable<QueuedMessage> GenerateQueuedMessages(SocketMessage arg)
		{
			string messageContent = this.CleanContent(arg.Content);

			string userCleaned = this._nameService.CleanUserName(arg.Author.GetDisplayName());

			ActiveChannel activeChannel = this._activeChannels.GetOrAdd(arg.Channel);

			await foreach (byte[] imageData in arg.GetImages())
			{
				yield return new QueuedMessage()
				{
					SocketMessage = arg,
					ChatEntry = new ChatEntry()
					{
						Image = imageData,
						SourceUser = userCleaned,
						SourceChannel = activeChannel.ChieName
					}
				};
			}

			if (!string.IsNullOrWhiteSpace(messageContent))
			{
				yield return new QueuedMessage()
				{
					SocketMessage = arg,
					ChatEntry = new ChatEntry()
					{
						Content = this.CleanContent(arg.Content),
						SourceUser = userCleaned,
						SourceChannel = activeChannel.ChieName
					}
				};
			}
		}

		private async Task LogIfDifferent(Exception ex)
		{
			if (this._lastError != ex.Message)
			{
				await this._logger.Write(ex.Message);
				this._lastError = ex.Message;
			}
		}
	}
}