using Discord.WebSocket;

namespace DiscordGpt.Models
{
    public class IncomingDiscordMessage
    {
        public string Author { get; set; }

        public string Channel { get; set; }

        public string Content { get; set; }

        public List<byte[]> Images { get; set; } = new List<byte[]>();

        public SocketMessage SocketMessage { get; set; }
    }
}