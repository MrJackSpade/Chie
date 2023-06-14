using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordGpt.Events
{
	public class ChieMessageSendEvent : EventArgs
	{
		public long MessageId { get; set; }
		public List<QueuedMessage> Messages { get; set; } = new ();
	}
}
