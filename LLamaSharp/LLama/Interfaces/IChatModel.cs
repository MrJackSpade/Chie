using LLama.Models;
using System.Collections.Generic;

namespace LLama.Interfaces
{
	public interface IChatModel
	{
		string Name { get; }

		IEnumerable<LlamaToken> Chat(string text, string? prompt = null);

		void InitChatAntiprompt(string[] antiprompt);

		/// <summary>
		/// Init a prompt for chat and automatically produce the next prompt during the chat.
		/// </summary>
		/// <param name="prompt"></param>
		void InitChatPrompt(string prompt);
	}
}