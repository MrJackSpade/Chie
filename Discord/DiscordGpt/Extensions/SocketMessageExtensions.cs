using Discord;
using Discord.WebSocket;

namespace DiscordGpt.Extensions
{
	public static class SocketMessageExtensions
	{
		private static readonly HttpClient _httpClient = new();

		public static async IAsyncEnumerable<byte[]> GetImages(this SocketMessage arg)
		{
			if (arg.Attachments.Any())
			{
				foreach (Attachment? attachment in arg.Attachments)
				{
					string mime = MimeMapping.MimeUtility.GetMimeMapping(attachment.Filename);

					if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
					{
						byte[] content = await _httpClient.GetByteArrayAsync(attachment.Url);

						yield return content;
					}
				}
			}
		}
	}
}