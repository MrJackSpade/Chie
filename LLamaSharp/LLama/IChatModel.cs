using System.Collections.Generic;

namespace LLama
{
	public interface IChatModel
	{
		string Name { get; }

		IEnumerable<string> Chat(string text, string? prompt = null, string encoding = "UTF-8");

		void InitChatAntiprompt(string[] antiprompt);

		/// <summary>
		/// Init a prompt for chat and automatically produce the next prompt during the chat.
		/// </summary>
		/// <param name="prompt"></param>
		void InitChatPrompt(string prompt, string encoding = "UTF-8");
	}
}