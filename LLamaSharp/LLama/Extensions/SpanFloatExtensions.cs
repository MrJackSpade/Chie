using Llama.Models;
using System;
using System.Collections.Generic;

namespace Llama.Extensions
{
	public static class SpanFloatExtensions
	{
		public static void Add(this Span<float> target, IEnumerable<KeyValuePair<int, float>> list)
		{
			foreach ((int key, float value) in list)
			{
				target[key] += value;
			}
		}

		public static Dictionary<LlamaToken, float> Extract(this Span<float> source, IEnumerable<LlamaToken> list)
		{
			Dictionary<LlamaToken, float> toReturn = new();

			foreach (LlamaToken llamaToken in list)
			{
				toReturn.Add(llamaToken, source[llamaToken.Id]);
			}

			return toReturn;
		}

		public static void Update(this Span<float> target, IEnumerable<KeyValuePair<LlamaToken, float>> list)
		{
			foreach (KeyValuePair<LlamaToken, float> llamaToken in list)
			{
				target[llamaToken.Key.Id] = llamaToken.Value;
			}
		}
	}
}