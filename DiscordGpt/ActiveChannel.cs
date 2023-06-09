using Discord.WebSocket;

namespace DiscordGpt
{
	public class ActiveChannel
	{
		private readonly SavedId _lastMessageId;

		private IDisposable _typingState;

		public ActiveChannel(ISocketMessageChannel channel)
		{
			this.Channel = channel;
			this.ChieName = $@"Discord:{channel.Id}";
			this._lastMessageId = new(this.ChieName + ".id");
		}

		public ISocketMessageChannel Channel { get; }

		public string ChieName { get; private set; }

		public ulong Id => this.Channel.Id;

		public long LastMessageId
		{
			get => this._lastMessageId.Value;
			set => this._lastMessageId.Value = value;
		}

		public void SetTypingState(bool state)
		{
			if (state)
			{
				this.StartTyping();
			}
			else
			{
				this.StopTyping();
			}
		}

		public void StartTyping()
		{
			this.StopTyping();
			this._typingState = this.Channel?.EnterTypingState();
		}

		public void StopTyping()
		{
			try
			{
				this._typingState?.Dispose();
				this._typingState = null;
			}
			catch
			{
			}
		}
	}
}