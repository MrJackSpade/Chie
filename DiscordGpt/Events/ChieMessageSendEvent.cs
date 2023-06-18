using DiscordGpt.Models;

namespace DiscordGpt.Events
{
    public class ChieMessageSendEvent : EventArgs
    {
        public long MessageId { get; set; }

        public List<QueuedMessage> Messages { get; set; } = new();
    }
}