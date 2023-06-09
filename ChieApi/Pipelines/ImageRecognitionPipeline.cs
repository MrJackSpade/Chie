using Blip.Client;
using Blip.Shared.Models;
using ChieApi.Interfaces;
using ChieApi.Shared.Entities;

namespace ChieApi.Pipelines
{
	public class ImageRecognitionPipeline : IRequestPipeline
	{
		private readonly BlipApiClient _blipClient;

		public ImageRecognitionPipeline(BlipApiClient clipClient)
		{
			this._blipClient = clipClient;
		}

		public async IAsyncEnumerable<ChatEntry> Process(ChatEntry chatEntry)
		{
			if (!chatEntry.HasImage)
			{
				yield return chatEntry;
				yield break;
			}

			DescribeResponse description = await this._blipClient.Describe(chatEntry.Image);

			if (description.Success)
			{
				yield return chatEntry with { Content = $"*Sends an image of {description.Content}*" };
			}
			else
			{
			}

			if (chatEntry.HasText)
			{
				yield return chatEntry with { Image = Array.Empty<byte>() };
			}
		}
	}
}