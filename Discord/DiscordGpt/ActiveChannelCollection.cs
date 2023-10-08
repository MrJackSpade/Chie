using Discord.WebSocket;
using DiscordGpt.Models;
using System.Collections;

namespace DiscordGpt
{
	public class ActiveChannelCollection : IEnumerable<ActiveChannel>
	{
		private readonly List<ActiveChannel> _channels = new();

		public ActiveChannel? this[ulong id] => this._channels.FirstOrDefault(c => c.Id == id);

		public ActiveChannel? this[string name] => this._channels.FirstOrDefault(c => c.ChieName == name);

		public ActiveChannel Add(ISocketMessageChannel channel)
		{
			ActiveChannel activeChannel = new(channel);
			this._channels.Add(activeChannel);
			return activeChannel;
		}

		public IEnumerator<ActiveChannel> GetEnumerator() => ((IEnumerable<ActiveChannel>)this._channels).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this._channels).GetEnumerator();

		public ActiveChannel GetOrAdd(ISocketMessageChannel channel) => this[channel.Id] ?? this.Add(channel);

		public bool TryGetValue(ulong id, out ActiveChannel? value)
		{
			value = this[id];
			return value != null;
		}
	}
}