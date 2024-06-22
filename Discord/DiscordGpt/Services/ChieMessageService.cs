using Ai.Utils;
using ChieApi.Client;
using ChieApi.Shared.Entities;
using ChieApi.Shared.Models;
using Discord;
using Discord.WebSocket;
using DiscordGpt.Constants;
using DiscordGpt.Events;
using DiscordGpt.Models;
using Logging.Shared.Extensions;
using Loxifi.AsyncExtensions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace DiscordGpt.Services
{
	public class ChieMessageService
	{
		public List<QueuedMessage> _outgoingMessageQueue = new();

		private readonly ChieClient _chieClient;

		private readonly ILogger _logger;

		private readonly DelayedTrigger _outgoingMessageTrigger;

		private string _lastError = string.Empty;

		public ChieMessageService(ChieClient chieClient, ILogger logger)
		{
			this._logger = logger;
			this._chieClient = chieClient;
			this._outgoingMessageTrigger = new DelayedTrigger(this.Flush, 3000, 30_000);
		}

		public event Func<ChieMessageSendEvent, Task> OnMessagesSent;

		public bool EnableSend
		{
			get => !this._outgoingMessageTrigger.Wait;
			set => this._outgoingMessageTrigger.Wait = !value;
		}

		public string CleanContent(string content)
		{
			string toReturn = content;

			//Remove custom emotes
			toReturn = Regex.Replace(toReturn, @"\<a?\:[a-zA-Z0-9\-]+\:\d+\>", "");

			//Remove newlines
			toReturn = toReturn.Replace("\r", "").Replace("\n", "");

            //remove non-ascii
            toReturn = Regex.Replace(toReturn, @"[^\x00-\x7F]+", string.Empty);

            return toReturn.Trim();
		}

		public async Task DeferredMessageProcessing(IncomingDiscordMessage arg)
		{
			try
			{
				QueuedMessage[] queuedMessages = await this.GenerateQueuedMessages(arg).ToArray();

				if (!queuedMessages.Any())
				{
					this._logger.LogInformation("No valid messages...");
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
				this._logger.LogError(ex.ToString());
			}
		}

		public async Task<bool> Flush()
		{
			this._logger.LogInformation("Sending messages to client...");

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
				this._logger.LogWarning("Messages reported as unseen. Client busy?");

				foreach (SocketMessage socketMessage in queuedMessages.Select(m => m.SocketMessage).Distinct())
				{
					await this.MarkUnseen(socketMessage);
				}
			}
			else
			{
				if (OnMessagesSent != null)
				{
					await OnMessagesSent.Invoke(new ChieMessageSendEvent()
					{
						MessageId = sendResponse.MessageId,
						Messages = queuedMessages
					});
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

		private async IAsyncEnumerable<QueuedMessage> GenerateQueuedMessages(IncomingDiscordMessage arg)
		{
			foreach (byte[] imageData in arg.Images)
			{
				yield return new QueuedMessage()
				{
					SocketMessage = arg.SocketMessage,
					ChatEntry = new ChatEntry()
					{
						Image = imageData,
						DisplayName = arg.DisplayName,
						UserId = arg.UserId,
						SourceChannel = arg.Channel
					}
				};
			}

			if (!string.IsNullOrWhiteSpace(arg.Content))
			{
				yield return new QueuedMessage()
				{
					SocketMessage = arg.SocketMessage,
					ChatEntry = new ChatEntry()
					{
						Content = this.CleanContent(arg.Content),
						DisplayName = arg.DisplayName,
						UserId = arg.UserId,
						SourceChannel = arg.Channel
					}
				};
			}
		}

		private async Task LogIfDifferent(Exception ex)
		{
			if (this._lastError != ex.Message)
			{
				this._logger.LogError(ex);
				this._lastError = ex.Message.ToString();
			}
		}
	}
}